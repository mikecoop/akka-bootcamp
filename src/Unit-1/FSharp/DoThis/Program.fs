open System
open Akka.FSharp
open WinTail
open WinTail.Actors

[<EntryPoint>]
let main argv =
    // initialize an actor system
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))

    // tell the consoleReader actor to begin
    consoleReaderActor <! Start

    myActorSystem.WhenTerminated.Wait ()
    0
