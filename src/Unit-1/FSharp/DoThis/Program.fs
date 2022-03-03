open System
open Akka.FSharp
open WinTail
open WinTail.Actors

[<EntryPoint>]
let main argv =
    // initialize an actor system
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let validationActor = spawn myActorSystem "validationActor" (actorOf2 (Actors.validationActor consoleWriterActor))
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor validationActor))

    // tell the consoleReader actor to begin
    consoleReaderActor <! Start

    myActorSystem.WhenTerminated.Wait ()
    0
