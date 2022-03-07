module Program

open System
open System.IO
open System.Linq
open System.Windows.Forms
open System.Xml.Linq
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Configuration.Hocon
open System.Configuration
open ChartApp

let hocon = XElement.Parse(File.ReadAllText(".\\akka-hocon.config"))
let config = ConfigurationFactory.ParseString(hocon.Descendants("hocon").Single().Value);
let chartActors = System.create "ChartActors" config

Application.EnableVisualStyles ()
Application.SetCompatibleTextRenderingDefault false

[<STAThread>]
do Application.Run (Form.load chartActors)