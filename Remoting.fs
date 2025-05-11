namespace ThesisManagementSystem

open WebSharper
open System
open System.IO
open System.Security.Cryptography
open System.Text

type User = {
    Username: string
    PasswordHash: string
    Salt: string
    IsAdmin: bool
    LastLogin: DateTime option
}

type Thesis = {
    Id: int
    Title: string
    Author: string
    Year: int
    Department: string
    Keywords: string list
    FilePath: string
}

type AuthResult =
    | Success of User
    | InvalidCredentials
    | UserNotFound
    | AccountLocked

module Server =
    let private users: Map<string, User> ref = ref Map.empty
    let private theses: Map<int, Thesis> ref = ref Map.empty
    let private loginAttempts: Map<string, int> ref = ref Map.empty
    let private MAX_LOGIN_ATTEMPTS: int = 3
    let private LOCKOUT_DURATION: TimeSpan = TimeSpan.FromMinutes(15)

    let generateSalt () =
        let bytes = Array.zeroCreate 16
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes(bytes)
        Convert.ToBase64String(bytes)

    let hashPassword (password: string) (salt: string) =
        use sha256 = SHA256.Create()
        let saltedPassword = password + salt
        let bytes = Encoding.UTF8.GetBytes(saltedPassword)
        let hash = sha256.ComputeHash(bytes)
        Convert.ToBase64String(hash)

    let addUser (user: User) =
        users.Value <- Map.add user.Username user users.Value

    let addThesis (thesis: Thesis) =
        theses.Value <- Map.add thesis.Id thesis theses.Value

    let private removeThesis (id: int) =
        theses.Value <- Map.remove id theses.Value

    let private checkLoginAttempts (username: string) =
        match Map.tryFind username loginAttempts.Value with
        | Some attempts when attempts >= MAX_LOGIN_ATTEMPTS ->
            AccountLocked
        | _ ->
            Success { Username = ""; PasswordHash = ""; Salt = ""; IsAdmin = false; LastLogin = None }

    let private resetLoginAttempts (username: string) =
        loginAttempts.Value <- Map.remove username loginAttempts.Value

    let private incrementLoginAttempts (username: string) =
        let currentAttempts =
            match Map.tryFind username loginAttempts.Value with
            | Some attempts -> attempts + 1
            | None -> 1
        loginAttempts.Value <- Map.add username currentAttempts loginAttempts.Value

    [<Rpc>]
    let Login (username: string) (password: string) =
        async {
            match checkLoginAttempts username with
            | AccountLocked ->
                return Error "Account is locked. Please try again later."
            | _ ->
                match Map.tryFind username users.Value with
                | Some user ->
                    let hashedPassword = hashPassword password user.Salt
                    if hashedPassword = user.PasswordHash then
                        resetLoginAttempts username
                        let updatedUser = { user with LastLogin = Some DateTime.Now }
                        addUser updatedUser
                        do! WebSharper.Web.Remoting.GetContext().UserSession.LoginUser(username, TimeSpan.FromHours(1))
                        return Ok "Login successful"
                    else
                        incrementLoginAttempts username
                        return Error "Invalid username or password"
                | None ->
                    incrementLoginAttempts username
                    return Error "Invalid username or password"
        }

    [<Rpc>]
    let Logout () =
        async {
            let ctx = WebSharper.Web.Remoting.GetContext()
            do! ctx.UserSession.Logout()
            return "Logged out successfully"
        }

    [<Rpc>]
    let SignupUser (username: string) (password: string) =
        async {
            if Map.containsKey username users.Value then
                return Error "Username already exists"
            else
                let salt = generateSalt()
                let passwordHash = hashPassword password salt
                let newUser = {
                    Username = username
                    PasswordHash = passwordHash
                    Salt = salt
                    IsAdmin = false
                    LastLogin = None
                }
                addUser newUser
                return Ok "User created successfully"
        }

    [<Rpc>]
    let GetUserInfo (username: string) =
        async {
            match Map.tryFind username users.Value with
            | Some user ->
                return Some {|
                    Username = user.Username
                    IsAdmin = user.IsAdmin
                    LastLogin = user.LastLogin
                |}
            | None ->
                return None
        }

    [<Rpc>]
    let GetTheses (searchTerm: string) (department: string) (year: int option) =
        async {
            let filteredTheses =
                theses.Value
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.filter (fun t ->
                    (String.IsNullOrEmpty(searchTerm) ||
                     t.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                     t.Keywords |> List.exists (fun k -> k.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))) &&
                    (String.IsNullOrEmpty(department) || t.Department = department) &&
                    (year.IsNone || t.Year = year.Value)
                )
            return List.ofSeq filteredTheses
        }

    [<Rpc>]
    let AddThesis (thesis: Thesis) =
        async {
            addThesis thesis
            return "Thesis added successfully"
        }

    [<Rpc>]
    let UpdateThesis (thesis: Thesis) =
        async {
            if Map.containsKey thesis.Id theses.Value then
                addThesis thesis
                return "Thesis updated successfully"
            else
                return "Thesis not found"
        }

    [<Rpc>]
    let DeleteThesis (id: int) =
        async {
            if Map.containsKey id theses.Value then
                removeThesis id
                return "Thesis deleted successfully"
            else
                return "Thesis not found"
        }

    [<Rpc>]
    let UploadThesis (title: string) (author: string) (year: int) (department: string) (keywords: string list) (fileContent: byte[]) =
        async {
            let id = if Map.isEmpty theses.Value then 1 else Map.keys theses.Value |> Seq.max |> (+) 1
            let fileName = sprintf "%d_%s.pdf" id (title.Replace(" ", "_"))
            let filePath = Path.Combine("wwwroot", "theses", fileName)

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore
            File.WriteAllBytes(filePath, fileContent)

            let thesis = {
                Id = id
                Title = title
                Author = author
                Year = year
                Department = department
                Keywords = keywords
                FilePath = "/theses/" + fileName
            }

            addThesis thesis
            return "Thesis uploaded successfully"
        }

    [<Rpc>]
    let InitializeSampleData () =
        async {
            // Add admin user
            let adminSalt = generateSalt()
            let adminUser = {
                Username = "admin"
                PasswordHash = hashPassword "P@$$w0rd" adminSalt
                Salt = adminSalt
                IsAdmin = true
                LastLogin = None
            }
            addUser adminUser

            // Add normal user
            let userSalt = generateSalt()
            let normalUser = {
                Username = "user"
                PasswordHash = hashPassword "user123" userSalt
                Salt = userSalt
                IsAdmin = false
                LastLogin = None
            }
            addUser normalUser

            // Add sample theses
            let sampleTheses = [
                {
                    Id = 1
                    Title = "Machine Learning in Healthcare"
                    Author = "John Smith"
                    Year = 2023
                    Department = "Computer Science"
                    Keywords = ["Machine Learning"; "Healthcare"; "AI"]
                    FilePath = "/theses/sample1.pdf"
                }
                {
                    Id = 2
                    Title = "Blockchain Technology and Its Applications"
                    Author = "Alice Johnson"
                    Year = 2022
                    Department = "Information Technology"
                    Keywords = ["Blockchain"; "Cryptocurrency"; "Security"]
                    FilePath = "/theses/sample2.pdf"
                }
                {
                    Id = 3
                    Title = "Renewable Energy Systems"
                    Author = "Michael Brown"
                    Year = 2021
                    Department = "Engineering"
                    Keywords = ["Renewable Energy"; "Sustainability"; "Solar Power"]
                    FilePath = "/theses/sample3.pdf"
                }
            ]

            for thesis in sampleTheses do
                addThesis thesis

            return "Sample data initialized successfully"
        }

    [<Rpc>]
    let GetAllUsers () =
        async {
            return users.Value
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.map (fun u -> {|
                    Username = u.Username
                    IsAdmin = u.IsAdmin
                    LastLogin = u.LastLogin |> Option.map (fun d -> d.ToString("yyyy-MM-dd hh:mm:ss tt")) |> Option.defaultValue "Never"
                |})
                |> List.ofSeq
        }

    [<Rpc>]
    let AddUser (username: string) (password: string) (isAdmin: bool) =
        async {
            if Map.containsKey username users.Value then
                return "Username already exists"
            else
                let salt = generateSalt()
                let passwordHash = hashPassword password salt
                let newUser = {
                    Username = username
                    PasswordHash = passwordHash
                    Salt = salt
                    IsAdmin = isAdmin
                    LastLogin = None
                }
                addUser newUser
                return "User added successfully"
        }

    [<Rpc>]
    let DeleteUser (username: string) =
        async {
            if Map.containsKey username users.Value then
                users.Value <- Map.remove username users.Value
                return "User deleted successfully"
            else
                return "User not found"
        }

    [<Rpc>]
    let GetDashboardStats () =
        async {
            let users = users.Value |> Map.toSeq |> Seq.map snd
            let theses = theses.Value |> Map.toSeq |> Seq.map snd

            let userStats = {|
                TotalUsers = users |> Seq.length
                AdminUsers = users |> Seq.filter (fun u -> u.IsAdmin) |> Seq.length
                RegularUsers = users |> Seq.filter (fun u -> not u.IsAdmin) |> Seq.length
                ActiveUsers = users |> Seq.filter (fun u -> u.LastLogin.IsSome) |> Seq.length
            |}

            let thesisStats = {|
                TotalTheses = theses |> Seq.length
                ThesesByYear = theses |> Seq.groupBy (fun t -> t.Year) |> Seq.map (fun (y, ts) -> y, ts |> Seq.length) |> Map.ofSeq
                ThesesByDepartment = theses |> Seq.groupBy (fun t -> t.Department) |> Seq.map (fun (d, ts) -> d, ts |> Seq.length) |> Map.ofSeq
            |}

            return {| UserStats = userStats; ThesisStats = thesisStats |}
        }

    type UserStats = {
        TotalUsers: int
        AdminUsers: int
        RegularUsers: int
        ActiveUsers: int
    }

    type ThesisStats = {
        TotalTheses: int
        ThesesByYear: Map<int, int>
        ThesesByDepartment: Map<string, int>
    }
