namespace Djambi.Api.Web.Model

[<CLIMutable>]
type BotInfoDto =
    {
        name : string
        description : string
    }

[<CLIMutable>]
type BotSelectCellRequestDto =
    {
        gameId : int
        playerId : int
    }

[<CLIMutable>]
type BotSelectCellResponseDto =
    {
        cellId : int
    }
