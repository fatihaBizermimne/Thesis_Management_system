namespace ThesisManagementSystem

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server
open System
open WebSharper.UI.Html

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/login">] Login
    | [<EndPoint "/signup">] Signup
    | [<EndPoint "/logout">] Logout
    | [<EndPoint "/theses">] Theses
    | [<EndPoint "/theses/search">] SearchTheses
    | [<EndPoint "/admin">] Admin
    | [<EndPoint "/admin/theses">] AdminTheses
    | [<EndPoint "/admin/users">] AdminUsers
    | [<EndPoint "/admin/dashboard">] AdminDashboard

module AuthHelper =
    let GetUsername (ctx: Context<EndPoint>) =
        ctx.UserSession.GetLoggedInUser()
        |> Async.RunSynchronously

    let IsAdmin (ctx: Context<EndPoint>) =
        async {
            match! ctx.UserSession.GetLoggedInUser() with
            | Some username ->
                let! userInfo = Server.GetUserInfo username
                return userInfo |> Option.map (fun u -> u.IsAdmin) |> Option.defaultValue false
            | None -> return false
        } |> Async.RunSynchronously

module Templating =
    open WebSharper.UI.Html

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
            let isActive = if endpoint = act then "nav-link active" else "nav-link"
            li [attr.``class`` "nav-item"] [
                a [
                    attr.``class`` isActive
                    attr.href (ctx.Link act)
                ] [text txt]
            ]
        [
            "Home" => EndPoint.Home
            "Theses" => EndPoint.Theses
            if AuthHelper.IsAdmin ctx then
                "Admin" => EndPoint.Admin
        ]

    let AuthMenu (ctx: Context<EndPoint>) : Doc list =
        match AuthHelper.GetUsername ctx with
        | Some username ->
            [
                li [attr.``class`` "nav-item"] [
                    span [attr.``class`` "nav-link"] [text (sprintf "Welcome, %s" username)]
                ]
                li [attr.``class`` "nav-item"] [
                    a [
                        attr.``class`` "nav-link"
                        attr.href (ctx.Link EndPoint.Logout)
                    ] [text "Logout"]
                ]
            ]
        | None ->
            [
                li [attr.``class`` "nav-item"] [
                    a [
                        attr.``class`` "nav-link"
                        attr.href (ctx.Link EndPoint.Login)
                    ] [text "Login"]
                ]
                li [attr.``class`` "nav-item"] [
                    a [
                        attr.``class`` "nav-link"
                        attr.href (ctx.Link EndPoint.Signup)
                    ] [text "Sign Up"]
                ]
            ]

    let Main ctx action (title: string) (body: Doc list) =
        Templates.MainTemplate()
            .Title(title)
            .MenuBar(MenuBar ctx action)
            .AuthMenu(AuthMenu ctx)
            .Body(body)
            .Doc()

module Site =
    open type WebSharper.UI.ClientServer

    let HomePage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.Home "Home" [
                match AuthHelper.GetUsername ctx with
                | None   -> client (Client.UnAuthorizedView())
                | Some _ -> client (Client.Home())
            ],
            Bundle = "home"
        )

    let LoginPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.Login "Login" [
                client (Client.Login())
            ]
        )

    let SignupPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.Signup "Sign Up" [
                client (Client.Signup())
            ]
        )

    let ThesesPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.Theses "Theses" [
                match AuthHelper.GetUsername ctx with
                | None   -> client (Client.UnAuthorizedView())
                | Some _ -> client (Client.Theses())
            ]
        )

    let SearchThesesPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.SearchTheses "Search Theses" [
                match AuthHelper.GetUsername ctx with
                | None   -> client (Client.UnAuthorizedView())
                | Some _ -> client (Client.SearchTheses())
            ]
        )

    let AdminPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.Admin "Admin Dashboard" [
                match AuthHelper.GetUsername ctx with
                | None -> client (Client.UnAuthorizedView())
                | Some _ ->
                    if AuthHelper.IsAdmin ctx then
                        client (Client.Admin())
                    else
                        div [attr.``class`` "container"] [
                            div [attr.``class`` "alert alert-danger"] [
                                text "Access denied. Admin privileges required."
                            ]
                        ]
            ]
        )

    let AdminThesesPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.AdminTheses "Manage Theses" [
                match AuthHelper.GetUsername ctx with
                | None -> client (Client.UnAuthorizedView())
                | Some _ ->
                    if AuthHelper.IsAdmin ctx then
                        client (Client.AdminTheses())
                    else
                        div [attr.``class`` "container"] [
                            div [attr.``class`` "alert alert-danger"] [
                                text "Access denied. Admin privileges required."
                            ]
                        ]
            ]
        )

    let AdminUsersPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.AdminUsers "Manage Users" [
                match AuthHelper.GetUsername ctx with
                | None -> client (Client.UnAuthorizedView())
                | Some _ ->
                    if AuthHelper.IsAdmin ctx then
                        client (Client.AdminUsers())
                    else
                        div [attr.``class`` "container"] [
                            div [attr.``class`` "alert alert-danger"] [
                                text "Access denied. Admin privileges required."
                            ]
                        ]
            ]
        )

    let AdminDashboardPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.AdminDashboard "Admin Dashboard" [
                match AuthHelper.GetUsername ctx with
                | None -> client (Client.UnAuthorizedView())
                | Some _ ->
                    if AuthHelper.IsAdmin ctx then
                        client (Client.AdminDashboard())
                    else
                        div [attr.``class`` "container"] [
                            div [attr.``class`` "alert alert-danger"] [
                                text "Access denied. Admin privileges required."
                            ]
                        ]
            ]
        )

    let LogoutPage (ctx: Context<EndPoint>) =
        async {
            do! ctx.UserSession.Logout()
            return! Content.RedirectTemporary EndPoint.Home
        }

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.Login -> LoginPage ctx
            | EndPoint.Signup -> SignupPage ctx
            | EndPoint.Theses -> ThesesPage ctx
            | EndPoint.SearchTheses -> SearchThesesPage ctx
            | EndPoint.Admin -> AdminPage ctx
            | EndPoint.AdminTheses -> AdminThesesPage ctx
            | EndPoint.AdminUsers -> AdminUsersPage ctx
            | EndPoint.AdminDashboard -> AdminDashboardPage ctx
            | EndPoint.Logout -> LogoutPage ctx
        )

