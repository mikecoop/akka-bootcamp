namespace WinTail

open System
open System.IO
open Akka.Actor
open Akka.FSharp
open WinTail.Messages

module Actors =
    type Command =
        | Start
        | Continue
        | Message of string
        | Exit

    let consoleReaderActor (mailbox: Actor<_>) message =
        // Print instructions to the console
        let doPrintInstructions () =
            Console.WriteLine "Please provide the URI of a log file on disk.\n"

        let (|Message|Exit|) (str: string) =
            match str.ToLower() with
            | "exit" -> Exit
            | _ -> Message(str)

        let getAndValidateInput () =
            let line = Console.ReadLine()
            match line with
            | Exit -> mailbox.Context.System.Terminate () |> ignore
            | _ -> select "/user/validationActor" mailbox.Context.System <! line

        match box message with
        | :? Command as command ->
            match command with
            | Start -> doPrintInstructions ()
            | _ -> ()
        | _ -> ()

        getAndValidateInput ()

    let tailActor (filePath: string) (reporter: IActorRef) (mailbox: Actor<_>) =
        // Monitor the file for changes
        let observer = new FileObserver (mailbox.Self, Path.GetFullPath (filePath))
        do observer.Start()

        // Read the initial contents of the file
        let fileStream = new FileStream (Path.GetFullPath filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let fileStreamReader = new StreamReader (fileStream, Text.Encoding.UTF8)
        let text = fileStreamReader.ReadToEnd ()
        do mailbox.Self <! InitialRead (filePath, text)

        // Ensure cleanup at the end of actor lifecycle
        mailbox.Defer <| fun () ->
            (observer :> IDisposable).Dispose()
            (fileStreamReader :> IDisposable).Dispose()
            (fileStream :> IDisposable).Dispose()

        let rec loop() = actor {
            let! message = mailbox.Receive ()
            match (box message) :?> FileCommand with
            | FileWrite(_) ->
                let text = fileStreamReader.ReadToEnd()
                if not <| String.IsNullOrWhiteSpace text
                then reporter <! text
                else ()
            | FileError (_, reason) -> reporter <! $"Tail error: {reason}"
            | InitialRead (_, text) -> reporter <! text
            return! loop()
        }
        loop()

    let tailCoordinatorActor (mailbox: Actor<_>) message =
        match message with
        | StartTail (filePath, reporter) ->
            spawn mailbox.Context "tailActor" (tailActor filePath reporter) |> ignore
        | _ -> ()

    let fileValidatorActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message =
        let (|IsFileUri|_|) path =
            if File.Exists path
            then Some path
            else None

        let (|EmptyMessage|Message|) (msg: string) =
            match msg.Length with
            | 0 -> EmptyMessage
            | _ -> Message(msg)

        match message with
        | EmptyMessage ->
            consoleWriter <! InputError ("Input was blank. Please try again.\n", ErrorType.Null)
            mailbox.Sender () <! Continue
        | IsFileUri _ ->
            consoleWriter <! InputSuccess ($"Start processing for {message}")
            select "/user/tailCoordinatorActor" mailbox.Context.System <! StartTail (message, consoleWriter)
        | _ ->
            consoleWriter <! InputError ($"{message} is not an existing URI on disk.", ErrorType.Validation)
            mailbox.Sender () <! Continue

    let consoleWriterActor message =
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine(message.ToString())
            Console.ResetColor()

        match box message with
        | :? InputResult as inputResult ->
            match inputResult with
            | InputError (reason, _) -> printInColor ConsoleColor.Red reason
            | InputSuccess reason -> printInColor ConsoleColor.Green reason
        | _ -> printInColor ConsoleColor.Yellow (message.ToString ())
