namespace ChartApp

open System.IO
open System.Linq
open System.Xml.Linq
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Drawing
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open Akka.FSharp.Spawn
open Akka.Util.Internal
open ChartApp

[<AutoOpen>]
module Form =
    let sysChart = new Chart(Name = "sysChart", Text = "sysChart", Dock = DockStyle.Fill, Location = Point(0, 0), Size = Size(684, 446), TabIndex = 0)
    let form = new Form(Name = "Main", Visible = true, Text = "System Metrics", AutoScaleDimensions = SizeF(6.F, 13.F), AutoScaleMode = AutoScaleMode.Font, ClientSize = Size(684, 446))
    let chartArea1 = new ChartArea(Name = "ChartArea1")
    let legend1 = new Legend(Name = "Legend1")

    // create the buttons
    let btnCpu = new Button(Name = "btnCpu", Text = "CPU (ON)", Location = Point(565, 275), Size = Size(110, 40), TabIndex = 1, UseVisualStyleBackColor = true, Anchor = (AnchorStyles.Right ||| AnchorStyles.Bottom))
    let btnMemory = new Button(Name = "btnMemory", Text = "MEMORY (OFF)", Location = Point(565, 320), Size = Size(110, 40), TabIndex = 2, UseVisualStyleBackColor = true, Anchor = (AnchorStyles.Right ||| AnchorStyles.Bottom))
    let btnDisk = new Button(Name = "btnDisk", Text = "DISK (OFF)", Location = Point(565, 365), Size = Size(110, 40), TabIndex = 3, UseVisualStyleBackColor = true, Anchor = (AnchorStyles.Right ||| AnchorStyles.Bottom))
    let btnPauseResume = new Button(Name = "btnPauseResume", Text = "Pause ||", Location = Point(565, 200), Size = Size(110, 40), TabIndex = 4, UseVisualStyleBackColor = true, Anchor = (AnchorStyles.Right ||| AnchorStyles.Bottom))

    sysChart.BeginInit ()
    form.SuspendLayout ()
    sysChart.ChartAreas.Add chartArea1
    sysChart.Legends.Add legend1

    // add buttons to the form
    form.Controls.Add btnPauseResume
    form.Controls.Add btnCpu
    form.Controls.Add btnMemory
    form.Controls.Add btnDisk

    form.Controls.Add sysChart

    sysChart.EndInit ()
    form.ResumeLayout false

    let load (myActorSystem:ActorSystem) =
        let chartActor = spawn myActorSystem "charting" (Actors.chartingActor sysChart btnPauseResume)
        let coordinatorActor = spawn myActorSystem "counters" (Actors.performanceCounterCoordinatorActor chartActor)
        let toggleActors = Map.ofList [
            (CounterType.Cpu, spawnOpt myActorSystem "cpuCounter" (Actors.buttonToggleActor coordinatorActor btnCpu CounterType.Cpu false) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
            (CounterType.Memory, spawnOpt myActorSystem "memoryCounter" (Actors.buttonToggleActor coordinatorActor btnMemory CounterType.Memory false) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
            (CounterType.Disk, spawnOpt myActorSystem "diskCounter" (Actors.buttonToggleActor coordinatorActor btnDisk CounterType.Disk false) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
        ]

        // The CPU counter will auto-start at launch
        toggleActors.[CounterType.Cpu] <! Toggle

        btnPauseResume.Click.Add (fun _ -> chartActor <! TogglePause)
        btnCpu.Click.Add (fun _ -> toggleActors.[CounterType.Cpu] <! Toggle)
        btnMemory.Click.Add (fun _ -> toggleActors.[CounterType.Memory] <! Toggle)
        btnDisk.Click.Add (fun _ -> toggleActors.[CounterType.Disk] <! Toggle)

        form