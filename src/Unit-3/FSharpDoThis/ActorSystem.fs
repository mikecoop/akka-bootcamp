namespace GithubActors

open System.IO
open System.Linq
open System.Xml.Linq
open Akka.Configuration
open Akka.FSharp

module ActorSystem =
    let hocon = XElement.Parse(File.ReadAllText(".\\akka-hocon.config"))
    let config = ConfigurationFactory.ParseString(hocon.Descendants("hocon").Single().Value);
    let githubActors = System.create "GithubActors" config