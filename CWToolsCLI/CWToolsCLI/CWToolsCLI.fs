namespace CWToolsCLI
open System.Text
open CWTools.Common
open System.IO

module CWToolsCLI =
    open Argu
    open Validator

    type Exiter() =
        interface IExiter with
            member __.Name = "paket exiter"
            member __.Exit (msg,code) =
                if code = ErrorCode.HelpText then
                    printfn "%s" msg ; exit 0
                else eprintfn "%s" msg ; exit 1

    type ListTypes =
        | Folders = 1
        | Files = 2
    type ListSort =
        | Path = 1
        | Time = 2
    type ListArgs =
        | [<MainCommand; ExactlyOnce; Last>] ListType of ListTypes
        | Sort of ListSort
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | ListType _ -> "Thing to list"
                | Sort _ -> "Sort by"
    type ValidateType =
        | ParseErrors = 1
        | Errors = 2
        | Warnings = 3
        | Info = 4
        | All = 5
    type ValidateArgs =
        | [<MainCommand; ExactlyOnce; Last>] ValType of ValidateType
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | ValType _ -> "Which errors to output"
    type Arguments =
        | Directory of path : string
        | Game of Game
        | [<CliPrefix(CliPrefix.None)>] Validate of ParseResults<ValidateArgs>
        | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>

    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Directory _ -> "specify the main game directory"
                | Game _ -> "specify the game"
                | Validate _ -> "Validate all mod files"
                | List _ -> "List things"

    let parser = ArgumentParser.Create<Arguments>(programName = "CWToolsCLI.exe", errorHandler = new Exiter())

    let list game directory (results : ParseResults<ListArgs>) =
        let gameObj = STL(directory)
        let sortOrder = results.GetResult <@ Sort @>
        match results.GetResult <@ ListType @> with
        | ListTypes.Folders -> printfn "%A" gameObj.folders
        | ListTypes.Files -> 
            match sortOrder with
            | ListSort.Path -> printfn "%A" gameObj.allFileList
            | ListSort.Time -> printfn "%A" (gameObj.allFileList |> List.sortByDescending (fun {time = t} -> t))
            | _ -> failwith "Unexpected sort order"
        | _ -> failwith "Unexpected list type"

    let validate game directory (results : ParseResults<_>) =
        let gameObj = STL(directory)
        match results.GetResult <@ ValType @> with
        | ValidateType.ParseErrors -> printfn "%A" gameObj.parserErrorList
        | ValidateType.Errors -> printfn "%A" gameObj.validationErrorList
        | ValidateType.All -> printfn "%A" gameObj.parserErrorList;  printfn "%A" gameObj.validationErrorList
        | _ -> failwith "Unexpected validation type"

    [<EntryPoint>]
    let main argv =
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        let results = parser.Parse argv
        let directory = results.GetResult <@ Directory @>
        let game = results.GetResult <@ Game @>
        match results.GetSubCommand() with
        | List r -> list game directory r
        | Validate r -> validate game directory r
        | Directory _
        | Game _ -> failwith "internal error: this code should never be reached"
        
        //printfn "%A" argv
        0 // return an integer exit code
