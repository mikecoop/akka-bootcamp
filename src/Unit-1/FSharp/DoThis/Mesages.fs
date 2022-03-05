namespace WinTail

open Akka.Actor

module Messages =
    // The user didn't define any input or the input was not valid.
    type ErrorType =
    | Null
    | Validation

    // Discriminated union to determine whether or not the user input was valid.
    type InputResult =
    | InputSuccess of string
    | InputError of reason: string * errorType: ErrorType

    // Messages to start and stop observing file content for any changes
    type TailCommand =
    | StartTail of filePath: string * reporterActor: IActorRef
    | StopTail of filePath: string

    type FileCommand =
    | FileWrite of fileName: string
    | FileError of fileName: string * reason: string
    | InitialRead of fileName: string * text: string