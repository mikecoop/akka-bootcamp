open System
open Akka.Actor
open Akka.FSharp
open WinTail
open WinTail.Actors

[<EntryPoint>]
let main argv =
    // initialize an actor system
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())

    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)

    // SupervisionStrategy used by tailCoordinatorActor
    let strategy () = Strategy.OneForOne ((fun ex ->
        match ex with
        | :? ArithmeticException -> Directive.Resume
        | :? NotSupportedException -> Directive.Stop
        | _ -> Directive.Restart), 10, TimeSpan.FromSeconds (30.))

    let tailCoordinatorActor = spawnOpt myActorSystem "tailCoordinatorActor" (actorOf2 Actors.tailCoordinatorActor) [ SpawnOption.SupervisorStrategy (strategy ()) ]

    let fileValidatorActor = spawn myActorSystem "validatorActor" (actorOf2 (Actors.fileValidatorActor consoleWriterActor))

    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 Actors.consoleReaderActor)

    // tell the consoleReader actor to begin
    consoleReaderActor <! Start

    myActorSystem.WhenTerminated.Wait ()
    0
