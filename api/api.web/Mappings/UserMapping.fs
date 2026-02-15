namespace Djambi.Api.Web.Mappings

open Djambi.Api.Model
open Djambi.Api.Web.Model

[<AutoOpen>]
module UserMapping =
    
    let toCreateUserRequest (source : CreateUserRequestDto) : CreateUserRequest =
        {
            name = source.name
            password = Some source.password
            email = None
        }

    let toUserDto (source : User) : UserDto =
        {
            id = source.id
            name = source.name
            privileges = source.privileges
        }

    let toQuickRegisterArgs (source : QuickRegisterRequestDto) : string * string option =
        let email =
            if System.String.IsNullOrWhiteSpace(source.email) then None
            else Some source.email
        (source.name, email)

    let toCreationSourceDto (source : CreationSource) : CreationSourceDto =
        {
            userId = source.userId
            userName = source.userName
            time = source.time
        }