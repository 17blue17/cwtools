namespace CWTools.Games
open CWTools.Common
open CWTools.Utilities.Position
open System.IO
open Files
open CWTools.Validation.Rules
open CWTools.Utilities.Utils
open CWTools.Validation.Stellaris.ScopeValidation

module LanguageFeatures =
    let makeEntityResourceInput (fileManager : FileManager) filepath filetext  =
        let filepath = Path.GetFullPath(filepath)
        let indexOfScope = filepath.IndexOf(fileManager.ScopeDirectory)
        let rootedpath =
            if indexOfScope = -1
            then filepath
            else filepath.Substring(indexOfScope + (fileManager.ScopeDirectory.Length))
        let logicalpath = fileManager.ConvertPathToLogicalPath rootedpath
        EntityResourceInput {scope = ""; filepath = filepath; logicalpath = logicalpath; filetext = filetext; validate = true}
    let makeFileWithContentResourceInput (fileManager : FileManager) filepath filetext  =
        let filepath = Path.GetFullPath(filepath)
        let indexOfScope = filepath.IndexOf(fileManager.ScopeDirectory)
        let rootedpath =
            if indexOfScope = -1
            then filepath
            else filepath.Substring(indexOfScope + (fileManager.ScopeDirectory.Length))
        let logicalpath = fileManager.ConvertPathToLogicalPath rootedpath
        FileWithContentResourceInput {scope = ""; filepath = filepath; logicalpath = logicalpath; filetext = filetext; validate = true}

    let completion (fileManager : FileManager) (completionService : CompletionService<_> option) (resourceManager : ResourceManager<_>) (pos : pos) (filepath : string) (filetext : string) =
        let split = filetext.Split('\n')
        let filetext = split |> Array.mapi (fun i s -> if i = (pos.Line - 1) then log (sprintf "%s" s); s.Insert(pos.Column, "x") else s) |> String.concat "\n"
        let resource = makeEntityResourceInput fileManager filepath filetext
        match resourceManager.ManualProcessResource resource, completionService with
        |Some e, Some completion ->
            log (sprintf "completion %A %A" (fileManager.ConvertPathToLogicalPath filepath) filepath)
            //log "scope at cursor %A" (getScopeContextAtPos pos lookup.scriptedTriggers lookup.scriptedEffects e.entity)
            completion.Complete(pos, e)
        |_, _ -> []


    let getInfoAtPos (fileManager : FileManager) (resourceManager : ResourceManager<_>) (infoService : FoldRules<_> option) (lookup : Lookup<_, _>) (pos : pos) (filepath : string) (filetext : string) =
        let resource = makeEntityResourceInput fileManager filepath filetext
        match resourceManager.ManualProcessResource resource, infoService with
        |Some e, Some info ->
            log (sprintf "getInfo %A %A" (fileManager.ConvertPathToLogicalPath filepath) filepath)
            match (info.GetInfo)(pos, e) with
            |Some (_, Some (t, tv)) ->
                lookup.typeDefInfo.[t] |> List.tryPick (fun (n, v) -> if n = tv then Some v else None)
            |_ -> None
        |_, _ -> None

    let findAllRefsFromPos (fileManager : FileManager) (resourceManager : ResourceManager<_>) (infoService : FoldRules<_> option) (pos : pos) (filepath : string) (filetext : string) =
        let resource = makeEntityResourceInput fileManager filepath filetext
        match resourceManager.ManualProcessResource resource, infoService with
        |Some e, Some info ->
            log (sprintf "findRefs %A %A" (fileManager.ConvertPathToLogicalPath filepath) filepath)
            match (info.GetInfo)(pos, e) with
            |Some (_, Some ((t : string), tv)) ->
                //log "tv %A %A" t tv
                let t = t.Split('.').[0]
                resourceManager.Api.ValidatableEntities() |> List.choose (fun struct(e, l) -> let x = l.Force().Referencedtypes in if x.IsSome then (x.Value.TryFind t) else ((info.GetReferencedTypes) e).TryFind t)
                               |> List.collect id
                               |> List.choose (fun (tvk, r) -> if tvk == tv then Some r else None)
                               |> Some
            |_ -> None
        |_, _ -> None

    let scopesAtPos (fileManager : FileManager) (resourceManager : ResourceManager<_>) (infoService : FoldRules<_> option) (anyScope : 'a) (pos : pos) (filepath : string) (filetext : string) =
        let resource = makeEntityResourceInput fileManager filepath filetext
        match resourceManager.ManualProcessResource resource, infoService with
        |Some e, Some info ->
            // match info.GetInfo(pos, e) with
            match (info.GetInfo)(pos, e) with
            |Some (ctx, _) when ctx <> { Root = anyScope; From = []; Scopes = [] } ->
                Some (ctx)
            |_ ->
                None
        |_ -> None