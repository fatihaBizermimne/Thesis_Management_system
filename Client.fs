namespace ThesisManagementSystem

open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.UI.Notation
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.JavaScript
open System

[<JavaScript>]
module Templates =

    type MainTemplate = Templating.Template<"Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript>]
module Client =

    type Session = {
        Username: string
        IsAdmin: bool
        LastLogin: string
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

    type DashboardStats = {
        UserStats: UserStats
        ThesisStats: ThesisStats
    }

    let mutable currentSession : Var<Option<Session>> = Var.Create None

    let Home () =
        div [attr.``class`` "container"] [
            div [attr.``class`` "jumbotron"] [
                h1 [] [text "Welcome to Thesis Management System"]
                p [] [text "Browse and search through our collection of academic theses."]
                div [] [
                    a [
                        attr.``class`` "btn btn-primary me-2"
                        attr.href "/theses"
                    ] [text "Browse Theses"]
                ]
            ]
        ]

    let Login () =
        let rvUsername = Var.Create ""
        let rvPassword = Var.Create ""
        let rvResult = Var.Create ""

        div [attr.``class`` "container"] [
            h2 [] [text "Login"]
            div [attr.``class`` "mb-3"] [
                label [attr.``class`` "form-label"] [text "Username"]
                Doc.InputType.Text [attr.``class`` "form-control"] rvUsername
            ]
            div [attr.``class`` "mb-3"] [
                label [attr.``class`` "form-label"] [text "Password"]
                Doc.InputType.Password [attr.``class`` "form-control"] rvPassword
            ]
            button [
                attr.``class`` "btn btn-primary"
                on.click (fun _ _ ->
                    async {
                        let! res = Server.Login rvUsername.Value rvPassword.Value
                        match res with
                        | Ok _ ->
                            rvResult := sprintf "Welcome, %s!" rvUsername.Value
                            JS.Window.Location.Assign("/")
                        | Error msg ->
                            rvResult := msg
                    }
                    |> Async.StartImmediate
                )
            ] [text "Login"]
            div [attr.``class`` "mb-3"] [
                p [attr.``class`` "form-text"] [textView rvResult.View]
            ]
            a [
                attr.``class`` "btn btn-link"
                attr.href "/signup"
            ] [text "Don't have an account? Sign up"]
        ]

    let Signup () =
        let rvUsername = Var.Create ""
        let rvPassword = Var.Create ""
        let rvConfirmPassword = Var.Create ""
        let rvResult = Var.Create ""

        div [attr.``class`` "container"] [
            h2 [] [text "Create an Account"]
            div [attr.``class`` "mb-3"] [
                label [attr.``class`` "form-label"] [text "Username"]
                Doc.InputType.Text [attr.``class`` "form-control"] rvUsername
            ]
            div [attr.``class`` "mb-3"] [
                label [attr.``class`` "form-label"] [text "Password"]
                Doc.InputType.Password [attr.``class`` "form-control"] rvPassword
            ]
            div [attr.``class`` "mb-3"] [
                label [attr.``class`` "form-label"] [text "Confirm Password"]
                Doc.InputType.Password [attr.``class`` "form-control"] rvConfirmPassword
            ]
            button [
                attr.``class`` "btn btn-primary"
                on.click (fun _ _ ->
                    async {
                        if rvPassword.Value = rvConfirmPassword.Value then
                            let! res = Server.SignupUser rvUsername.Value rvPassword.Value
                            match res with
                            | Ok msg ->
                                rvResult := msg
                                JS.Window.Location.Assign("/login")
                            | Error msg ->
                                rvResult := msg
                        else
                            rvResult := "Passwords do not match."
                    }
                    |> Async.StartImmediate
                )
            ] [text "Sign Up"]
            div [attr.``class`` "mb-3"] [
                p [attr.``class`` "form-text"] [textView rvResult.View]
            ]
            a [
                attr.``class`` "btn btn-link"
                attr.href "/login"
            ] [text "Already have an account? Login"]
        ]

    let Theses () =
        let rvTheses = Var.Create List.empty<ThesisManagementSystem.Thesis>

        // Fetch all theses when the view is loaded
        async {
            let! theses = Server.GetTheses "" "" None
            rvTheses := theses
        } |> Async.StartImmediate

        div [attr.``class`` "container"] [
            div [attr.``class`` "jumbotron"] [
                h1 [] [text "Theses"]
                p [] [text "Browse and search through our collection of academic theses."]
                div [] [
                    a [
                        attr.``class`` "btn btn-primary me-2"
                        attr.href "/theses/search"
                    ] [text "Search Theses"]
                ]
            ]
            div [attr.``class`` "mt-4"] [
                h2 [] [text "All Theses"]
                Doc.BindSeqCached (fun (thesis: ThesisManagementSystem.Thesis) ->
                    div [attr.``class`` "card mb-3"] [
                        div [attr.``class`` "card-body"] [
                            h5 [attr.``class`` "card-title"] [text thesis.Title]
                            p [attr.``class`` "card-text"] [
                                text (sprintf "Author: %s" thesis.Author)
                                br [] []
                                text (sprintf "Year: %d" thesis.Year)
                                br [] []
                                text (sprintf "Department: %s" thesis.Department)
                                br [] []
                                text (sprintf "Keywords: %s" (String.Join(", ", thesis.Keywords)))
                            ]
                            a [
                                attr.``class`` "btn btn-primary"
                                attr.href thesis.FilePath
                                attr.target "_blank"
                            ] [text "View Thesis"]
                            a [
                                attr.``class`` "btn btn-success ms-2"
                                attr.href thesis.FilePath
                                attr.download ""
                            ] [
                                i [attr.``class`` "bi bi-download me-1"] []
                                text "Download"
                            ]
                        ]
                    ]
                ) rvTheses.View
            ]
        ]

    let Admin () =
        div [attr.``class`` "container"] [
            div [attr.``class`` "jumbotron"] [
                h1 [] [text "Admin Dashboard"]
                p [] [text "Manage theses and users."]
                div [] [
                    a [
                        attr.``class`` "btn btn-primary me-2"
                        attr.href "/admin/dashboard"
                    ] [text "View Dashboard"]
                    a [
                        attr.``class`` "btn btn-primary me-2"
                        attr.href "/admin/theses"
                    ] [text "Manage Theses"]
                    a [
                        attr.``class`` "btn btn-primary"
                        attr.href "/admin/users"
                    ] [text "Manage Users"]
                ]
            ]
        ]

    // Show this view if the user is not logged in
    // Show login and signup buttons
    let UnAuthorizedView () =
        div [attr.``class`` "container"] [
            div [attr.``class`` "jumbotron"] [
                h1 [] [text "Welcome to Thesis Management System"]
                div [] [
                    a [
                        attr.``class`` "btn btn-primary me-2"
                        attr.href "/login"
                    ] [text "Login"]
                    a [
                        attr.``class`` "btn btn-primary"
                        attr.href "/signup"
                    ] [text "Sign Up"]
                ]
            ]
        ]

    let SearchTheses () =
        let rvSearchTerm = Var.Create ""
        let rvDepartment = Var.Create ""
        let rvYear = Var.Create ""
        let rvTheses = Var.Create List.empty<ThesisManagementSystem.Thesis>
        let departments = ["Computer Science"; "Information Technology"; "Engineering"; "Mathematics"; "Physics"]

        let search () =
            async {
                let year = if String.IsNullOrEmpty(rvYear.Value) then None else Some (int rvYear.Value)
                let! theses = Server.GetTheses rvSearchTerm.Value rvDepartment.Value year
                rvTheses.Value <- theses
            } |> Async.StartImmediate

        div [attr.``class`` "container"] [
            div [attr.``class`` "jumbotron"] [
                h1 [] [text "Search Theses"]
                p [] [text "Search through our collection of academic theses."]
            ]
            div [attr.``class`` "row mt-4"] [
                div [attr.``class`` "col-md-4"] [
                    div [attr.``class`` "card"] [
                        div [attr.``class`` "card-body"] [
                            h5 [attr.``class`` "card-title"] [text "Search Filters"]
                            div [attr.``class`` "mb-3"] [
                                label [attr.``class`` "form-label"] [text "Search Term"]
                                Doc.InputType.Text [
                                    attr.``class`` "form-control"
                                    attr.placeholder "Title or keywords"
                                ] rvSearchTerm
                            ]
                            div [attr.``class`` "mb-3"] [
                                label [attr.``class`` "form-label"] [text "Department"]
                                Doc.InputType.Select [
                                    attr.``class`` "form-select"
                                ] id ("" :: departments) rvDepartment
                            ]
                            div [attr.``class`` "mb-3"] [
                                label [attr.``class`` "form-label"] [text "Year"]
                                Doc.InputType.Text [
                                    attr.``class`` "form-control"
                                    attr.placeholder "e.g. 2023"
                                ] rvYear
                            ]
                            button [
                                attr.``class`` "btn btn-primary w-100"
                                on.click (fun _ _ -> search())
                            ] [text "Search"]
                        ]
                    ]
                ]
                div [attr.``class`` "col-md-8"] [
                    h2 [] [text "Search Results"]
                    Doc.BindSeqCached (fun thesis ->
                        div [attr.``class`` "card mb-3"] [
                            div [attr.``class`` "card-body"] [
                                h5 [attr.``class`` "card-title"] [text thesis.Title]
                                p [attr.``class`` "card-text"] [
                                    text (sprintf "Author: %s" thesis.Author)
                                    br [] []
                                    text (sprintf "Year: %d" thesis.Year)
                                    br [] []
                                    text (sprintf "Department: %s" thesis.Department)
                                    br [] []
                                    text (sprintf "Keywords: %s" (String.Join(", ", thesis.Keywords)))
                                ]
                                a [
                                    attr.``class`` "btn btn-primary"
                                    attr.href thesis.FilePath
                                    attr.target "_blank"
                                ] [text "View Thesis"]
                                a [
                                    attr.``class`` "btn btn-success ms-2"
                                    attr.href thesis.FilePath
                                    attr.download ""
                                ] [
                                    i [attr.``class`` "bi bi-download me-1"] []
                                    text "Download"
                                ]
                            ]
                        ]
                    ) rvTheses.View
                ]
            ]
        ]

    let AdminTheses () =
        let rvTheses = Var.Create List.empty<ThesisManagementSystem.Thesis>
        let rvTitle = Var.Create ""
        let rvAuthor = Var.Create ""
        let rvYear = Var.Create ""
        let rvDepartment = Var.Create ""
        let rvKeywords = Var.Create ""
        let rvFile = Var.Create None
        let rvMessage = Var.Create ""
        let rvEditingThesis = Var.Create None
        let departments = [""; "Computer Science"; "Information Technology"; "Engineering"; "Mathematics"; "Physics"]

        // Load all theses when the view is loaded
        async {
            let! theses = Server.GetTheses "" "" None
            rvTheses := theses
        } |> Async.StartImmediate

        let clearForm () =
            rvTitle := ""
            rvAuthor := ""
            rvYear := ""
            rvDepartment := ""
            rvKeywords := ""
            rvFile := None
            rvMessage := ""
            rvEditingThesis := None

        let validateForm () =
            if String.IsNullOrWhiteSpace(rvTitle.Value) then
                Some "Title is required"
            elif String.IsNullOrWhiteSpace(rvAuthor.Value) then
                Some "Author is required"
            elif String.IsNullOrWhiteSpace(rvYear.Value) || not (Int32.TryParse(rvYear.Value, ref 0)) then
                Some "Valid year is required"
            elif String.IsNullOrWhiteSpace(rvDepartment.Value) then
                Some "Department is required"
            elif String.IsNullOrWhiteSpace(rvKeywords.Value) then
                Some "Keywords are required"
            elif rvFile.Value.IsNone && rvEditingThesis.Value.IsNone then
                Some "File is required for new theses"
            else
                None

        let startEditing (thesis: ThesisManagementSystem.Thesis) =
            rvTitle := thesis.Title
            rvAuthor := thesis.Author
            rvYear := string thesis.Year
            rvDepartment := thesis.Department
            rvKeywords := String.Join(", ", thesis.Keywords)
            rvEditingThesis := Some thesis

        div [attr.``class`` "container"] [
            h1 [] [text "Manage Theses"]

            // Add/Edit Thesis Form
            div [attr.``class`` "card mb-4"] [
                div [attr.``class`` "card-header"] [
                    Doc.BindView (fun editing ->
                        h4 [] [
                            let headingText =
                                match editing with
                                | Some _ -> "Edit Thesis"
                                | None   -> "Add New Thesis"

                            text headingText
                        ]
                    ) rvEditingThesis.View
                ]
                div [attr.``class`` "card-body"] [
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Title"]
                        Doc.InputType.Text [attr.``class`` "form-control"] rvTitle
                    ]
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Author"]
                        Doc.InputType.Text [attr.``class`` "form-control"] rvAuthor
                    ]
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Year"]
                        Doc.InputType.Text [
                            attr.``class`` "form-control"
                            attr.``type`` "number"
                        ] rvYear
                    ]
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Department"]
                        Doc.InputType.Select [
                            attr.``class`` "form-select"
                        ] id departments rvDepartment
                    ]
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Keywords (comma-separated)"]
                        Doc.InputType.Text [attr.``class`` "form-control"] rvKeywords
                    ]
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Thesis File (PDF)"]
                        input [
                            attr.``type`` "file"
                            attr.``class`` "form-control"
                            attr.accept ".pdf"
                            on.change (fun elem event ->
                                let input = event.Target :?> HTMLInputElement
                                if input.Files.Length > 0 then
                                    rvFile := Some input.Files.[0]
                            )
                        ] Seq.empty
                    ]
                    div [attr.``class`` "btn-group"] [
                        Doc.BindView (fun editing ->
                            button [
                                attr.``class`` "btn btn-primary"
                                on.click (fun _ _ ->
                                    async {
                                        match validateForm() with
                                        | Some error ->
                                            rvMessage.Value <- error
                                        | None ->
                                            let keywords = rvKeywords.Value.Split(',')
                                                         |> Array.map (fun k -> k.Trim())
                                                         |> Array.toList

                                            match editing with
                                            | Some thesis ->
                                                // Update existing thesis
                                                let updatedThesis = {
                                                    thesis with
                                                        Title = rvTitle.Value
                                                        Author = rvAuthor.Value
                                                        Year = int rvYear.Value
                                                        Department = rvDepartment.Value
                                                        Keywords = keywords
                                                }
                                                let! result = Server.UpdateThesis updatedThesis
                                                rvMessage := result
                                                if result = "Thesis updated successfully" then
                                                    clearForm()
                                                    let! updatedTheses = Server.GetTheses "" "" None
                                                    rvTheses := updatedTheses
                                            | None ->
                                                // Add new thesis
                                                match rvFile.Value with
                                                | Some file ->
                                                    let! arrayBuffer = file.ArrayBuffer().AsAsync()
                                                    let bytes = Array.zeroCreate<byte> (int arrayBuffer.ByteLength)
                                                    let view = new Uint8Array(arrayBuffer)
                                                    for i in 0 .. bytes.Length - 1 do
                                                        bytes.[i] <- view.Get(i)

                                                    let! result = Server.UploadThesis
                                                                    rvTitle.Value
                                                                    rvAuthor.Value
                                                                    (int rvYear.Value)
                                                                    rvDepartment.Value
                                                                    keywords
                                                                    bytes
                                                    rvMessage := result
                                                    if result = "Thesis uploaded successfully" then
                                                        clearForm()
                                                        let! updatedTheses = Server.GetTheses "" "" None
                                                        rvTheses := updatedTheses
                                                | None ->
                                                    rvMessage := "File is required for new theses"
                                    } |> Async.StartImmediate
                                )
                            ] [
                                let buttonText =
                                    match editing with
                                    | Some _ -> "Update Thesis"
                                    | None   -> "Add Thesis"

                                text buttonText
                            ]
                        ) rvEditingThesis.View
                        Doc.BindView (fun editing ->
                            match editing with
                            | Some _ ->
                                button [
                                    attr.``class`` "btn btn-secondary"
                                    on.click (fun _ _ -> clearForm())
                                ] [text "Cancel"]
                            | None ->
                                Doc.Empty
                        ) rvEditingThesis.View
                    ]
                    div [attr.``class`` "mt-3"] [
                        Doc.Concat [
                            if not (String.IsNullOrEmpty(rvMessage.Value)) then
                                div [
                                    attr.``class`` (
                                        if rvMessage.Value.Contains("successfully") then
                                            "alert alert-success"
                                        else
                                            "alert alert-danger"
                                    )
                                ] [
                                    text rvMessage.Value
                                ]
                        ]
                    ]
                ]
            ]

            // List of Existing Theses
            div [attr.``class`` "card"] [
                div [attr.``class`` "card-header"] [
                    h4 [] [text "Existing Theses"]
                ]
                div [attr.``class`` "card-body"] [
                    Doc.BindSeqCached (fun (thesis: ThesisManagementSystem.Thesis) ->
                        div [attr.``class`` "card mb-3"] [
                            div [attr.``class`` "card-body"] [
                                h5 [attr.``class`` "card-title"] [text thesis.Title]
                                p [attr.``class`` "card-text"] [
                                    text (sprintf "Author: %s" thesis.Author)
                                    br [] []
                                    text (sprintf "Year: %d" thesis.Year)
                                    br [] []
                                    text (sprintf "Department: %s" thesis.Department)
                                    br [] []
                                    text (sprintf "Keywords: %s" (String.Join(", ", thesis.Keywords)))
                                ]
                                div [attr.``class`` "btn-group"] [
                                    a [
                                        attr.``class`` "btn btn-primary"
                                        attr.href thesis.FilePath
                                        attr.target "_blank"
                                    ] [text "View"]
                                    a [
                                        attr.``class`` "btn btn-success"
                                        attr.href thesis.FilePath
                                        attr.download ""
                                    ] [
                                        i [attr.``class`` "bi bi-download me-1"] []
                                        text "Download"
                                    ]
                                    button [
                                        attr.``class`` "btn btn-warning"
                                        on.click (fun _ _ -> startEditing thesis)
                                    ] [text "Edit"]
                                    button [
                                        attr.``class`` "btn btn-danger"
                                        on.click (fun _ _ ->
                                            async {
                                                let! result = Server.DeleteThesis thesis.Id
                                                rvMessage.Value <- result
                                                if result = "Thesis deleted successfully" then
                                                    let! updatedTheses = Server.GetTheses "" "" None
                                                    rvTheses := updatedTheses
                                            } |> Async.StartImmediate
                                        )
                                    ] [text "Delete"]
                                ]
                            ]
                        ]
                    ) rvTheses.View
                ]
            ]
        ]

    let AdminUsers () =
        let rvUsers = Var.Create List.empty<Session>
        let rvUsername = Var.Create ""
        let rvPassword = Var.Create ""
        let rvIsAdmin = Var.Create false
        let rvMessage = Var.Create ""

        // Load all users when the view is loaded
        async {
            let! users = Server.GetAllUsers()
            let userSessions =
                users
                |> List.map (fun u ->
                    { Username = u.Username
                      IsAdmin = u.IsAdmin
                      LastLogin = u.LastLogin})
            rvUsers.Value <- userSessions
        } |> Async.StartImmediate

        let clearForm () =
            rvUsername := ""
            rvPassword := ""
            rvIsAdmin := false
            rvMessage := ""

        let validateForm () =
            if String.IsNullOrWhiteSpace(rvUsername.Value) then
                Some "Username is required"
            elif String.IsNullOrWhiteSpace(rvPassword.Value) then
                Some "Password is required"
            else
                None

        div [attr.``class`` "container"] [
            h1 [] [text "Manage Users"]

            // Add New User Form
            div [attr.``class`` "card mb-4"] [
                div [attr.``class`` "card-header"] [
                    h4 [] [text "Add New User"]
                ]
                div [attr.``class`` "card-body"] [
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Username"]
                        Doc.InputType.Text [attr.``class`` "form-control"] rvUsername
                    ]
                    div [attr.``class`` "mb-3"] [
                        label [attr.``class`` "form-label"] [text "Password"]
                        Doc.InputType.Password [attr.``class`` "form-control"] rvPassword
                    ]
                    div [attr.``class`` "mb-3"] [
                        div [attr.``class`` "form-check"] [
                            Doc.InputType.CheckBox [attr.``class`` "form-check-input"] rvIsAdmin
                            label [attr.``class`` "form-check-label"] [text "Admin User"]
                        ]
                    ]
                    button [
                        attr.``class`` "btn btn-primary"
                        on.click (fun _ _ ->
                            async {
                                match validateForm() with
                                | Some error ->
                                    rvMessage.Value <- error
                                | None ->
                                    let! result = Server.AddUser rvUsername.Value rvPassword.Value rvIsAdmin.Value
                                    rvMessage.Value <- result
                                    if result = "User added successfully" then
                                        clearForm()
                                        let! updatedUsers =
                                            Server.GetAllUsers()

                                        let userSessions =
                                            updatedUsers
                                            |> List.map (fun u ->
                                                { Username = u.Username
                                                  IsAdmin = u.IsAdmin
                                                  LastLogin = u.LastLogin })

                                        rvUsers.Value <- userSessions
                            } |> Async.StartImmediate
                        )
                    ] [text "Add User"]
                    div [attr.``class`` "mt-3"] [
                        Doc.Concat [
                            if not (String.IsNullOrEmpty(rvMessage.Value)) then
                                div [
                                    attr.``class`` (
                                        if rvMessage.Value.Contains("successfully") then
                                            "alert alert-success"
                                        else
                                            "alert alert-danger"
                                    )
                                ] [
                                    text rvMessage.Value
                                ]
                        ]
                    ]
                ]
            ]

            // List of Existing Users
            div [attr.``class`` "card"] [
                div [attr.``class`` "card-header"] [
                    h4 [] [text "Existing Users"]
                ]
                div [attr.``class`` "card-body"] [
                    Doc.BindSeqCached (fun user ->
                        div [attr.``class`` "card mb-3"] [
                            div [attr.``class`` "card-body"] [
                                h5 [attr.``class`` "card-title"] [text user.Username]
                                p [attr.``class`` "card-text"] [
                                    text (sprintf "Role: %s" (if user.IsAdmin then "Admin" else "User"))
                                    br [] []
                                    text (sprintf "Last Login: %s" user.LastLogin)
                                ]
                                button [
                                    attr.``class`` "btn btn-danger"
                                    on.click (fun _ _ ->
                                        async {
                                            let! result = Server.DeleteUser user.Username
                                            rvMessage.Value <- result
                                            if result = "User deleted successfully" then
                                                let! updatedUsers = Server.GetAllUsers()
                                                let userSessions =
                                                    updatedUsers
                                                    |> List.map (fun u ->
                                                        { Username = u.Username
                                                          IsAdmin = u.IsAdmin
                                                          LastLogin = u.LastLogin })
                                                rvUsers.Value <- userSessions
                                        } |> Async.StartImmediate
                                    )
                                ] [text "Delete"]
                            ]
                        ]
                    ) rvUsers.View
                ]
            ]
        ]

    let AdminDashboard () =
        let rvStats = Var.Create<DashboardStats option> None
        let rvUsers = Var.Create<{| Username: string; IsAdmin: bool; LastLogin: string |} list> []
        let rvTheses = Var.Create<Thesis list> []

        async {
            let! stats = Server.GetDashboardStats()
            let! users = Server.GetAllUsers()
            let! theses = Server.GetTheses "" "" None
            let dashboardStats = {
                UserStats = {
                    TotalUsers = stats.UserStats.TotalUsers
                    AdminUsers = stats.UserStats.AdminUsers
                    RegularUsers = stats.UserStats.RegularUsers
                    ActiveUsers = stats.UserStats.ActiveUsers
                }
                ThesisStats = {
                    TotalTheses = stats.ThesisStats.TotalTheses
                    ThesesByYear = stats.ThesisStats.ThesesByYear
                    ThesesByDepartment = stats.ThesisStats.ThesesByDepartment
                }
            }
            rvStats.Value <- Some dashboardStats
            rvUsers.Value <- users
            rvTheses.Value <- theses
        }
        |> Async.Start

        let renderUserStats (stats: UserStats) =
            div [attr.``class`` "row g-4"] [
                div [attr.``class`` "col-md-3"] [
                    div [attr.``class`` "card h-100 border-primary"] [
                        div [attr.``class`` "card-body text-center"] [
                            div [attr.``class`` "display-4 text-primary mb-3"] [
                                i [attr.``class`` "bi bi-people-fill"] []
                            ]
                            h5 [attr.``class`` "card-title"] [text "Total Users"]
                            p [attr.``class`` "card-text display-6"] [text (string stats.TotalUsers)]
                        ]
                    ]
                ]
                div [attr.``class`` "col-md-3"] [
                    div [attr.``class`` "card h-100 border-success"] [
                        div [attr.``class`` "card-body text-center"] [
                            div [attr.``class`` "display-4 text-success mb-3"] [
                                i [attr.``class`` "bi bi-shield-check"] []
                            ]
                            h5 [attr.``class`` "card-title"] [text "Admin Users"]
                            p [attr.``class`` "card-text display-6"] [text (string stats.AdminUsers)]
                        ]
                    ]
                ]
                div [attr.``class`` "col-md-3"] [
                    div [attr.``class`` "card h-100 border-info"] [
                        div [attr.``class`` "card-body text-center"] [
                            div [attr.``class`` "display-4 text-info mb-3"] [
                                i [attr.``class`` "bi bi-person-fill"] []
                            ]
                            h5 [attr.``class`` "card-title"] [text "Regular Users"]
                            p [attr.``class`` "card-text display-6"] [text (string stats.RegularUsers)]
                        ]
                    ]
                ]
                div [attr.``class`` "col-md-3"] [
                    div [attr.``class`` "card h-100 border-warning"] [
                        div [attr.``class`` "card-body text-center"] [
                            div [attr.``class`` "display-4 text-warning mb-3"] [
                                i [attr.``class`` "bi bi-activity"] []
                            ]
                            h5 [attr.``class`` "card-title"] [text "Active Users"]
                            p [attr.``class`` "card-text display-6"] [text (string stats.ActiveUsers)]
                        ]
                    ]
                ]
            ]

        let renderThesisStats (stats: ThesisStats) =
            div [attr.``class`` "row g-4 mt-4"] [
                div [attr.``class`` "col-md-4"] [
                    div [attr.``class`` "card h-100 border-primary"] [
                        div [attr.``class`` "card-body text-center"] [
                            div [attr.``class`` "display-4 text-primary mb-3"] [
                                i [attr.``class`` "bi bi-journal-text"] []
                            ]
                            h5 [attr.``class`` "card-title"] [text "Total Theses"]
                            p [attr.``class`` "card-text display-6"] [text (string stats.TotalTheses)]
                        ]
                    ]
                ]
                div [attr.``class`` "col-md-4"] [
                    div [attr.``class`` "card h-100"] [
                        div [attr.``class`` "card-header bg-primary text-white"] [
                            h5 [attr.``class`` "card-title mb-0"] [
                                i [attr.``class`` "bi bi-calendar3"] []
                                text "Theses by Year"
                            ]
                        ]
                        div [attr.``class`` "card-body"] [
                            div [attr.``class`` "table-responsive"] [
                                table [attr.``class`` "table table-hover"] [
                                    thead [] [
                                        tr [] [
                                            th [] [text "Year"]
                                            th [] [text "Count"]
                                        ]
                                    ]
                                    tbody [] [
                                        for (year, count) in stats.ThesesByYear |> Map.toSeq |> Seq.sortByDescending fst do
                                            tr [] [
                                                td [] [text (string year)]
                                                td [] [text (string count)]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                div [attr.``class`` "col-md-4"] [
                    div [attr.``class`` "card h-100"] [
                        div [attr.``class`` "card-header bg-primary text-white"] [
                            h5 [attr.``class`` "card-title mb-0"] [
                                i [attr.``class`` "bi bi-building"] []
                                text "Theses by Department"
                            ]
                        ]
                        div [attr.``class`` "card-body"] [
                            div [attr.``class`` "table-responsive"] [
                                table [attr.``class`` "table table-hover"] [
                                    thead [] [
                                        tr [] [
                                            th [] [text "Department"]
                                            th [] [text "Count"]
                                        ]
                                    ]
                                    tbody [] [
                                        for (dept, count) in stats.ThesesByDepartment |> Map.toSeq |> Seq.sortByDescending snd do
                                            tr [] [
                                                td [] [text dept]
                                                td [] [text (string count)]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

        div [attr.``class`` "container-fluid py-4"] [
            div [attr.``class`` "d-flex justify-content-between align-items-center mb-4"] [
                h1 [attr.``class`` "display-4"] [
                    i [attr.``class`` "bi bi-speedometer2"] []
                    text "Admin Dashboard"
                ]
                div [] [
                    a [
                        attr.``class`` "btn btn-outline-primary me-2"
                        attr.href "/admin/theses"
                    ] [
                        i [attr.``class`` "bi bi-journal-text"] []
                        text "Manage Theses"
                    ]
                    a [
                        attr.``class`` "btn btn-outline-primary"
                        attr.href "/admin/users"
                    ] [
                        i [attr.``class`` "bi bi-people"] []
                        text "Manage Users"
                    ]
                ]
            ]
            div [attr.``class`` "row"] [
                div [attr.``class`` "col-md-12"] [
                    Doc.BindView (fun (stats: DashboardStats option) ->
                        match stats with
                        | Some s ->
                            div [] [
                                renderUserStats s.UserStats
                                renderThesisStats s.ThesisStats
                            ]
                        | None ->
                            div [attr.``class`` "text-center py-5"] [
                                div [attr.``class`` "spinner-border text-primary"] []
                                p [attr.``class`` "mt-3"] [text "Loading dashboard statistics..."]
                            ]
                    ) rvStats.View
                ]
            ]
        ]
