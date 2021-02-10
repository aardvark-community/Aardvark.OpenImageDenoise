#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"
#load @"paket-files/build/aardvark-platform/aardvark.fake/DefaultSetup.fsx"

open System
open System.IO
open System.Diagnostics
open Aardvark.Fake
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators

do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

DefaultSetup.install ["Aardvark.OpenImageDenoise.sln"]


#if DEBUG
do System.Diagnostics.Debugger.Launch() |> ignore
#endif

Target.create "PushDev" (fun _ -> 
    DefaultSetup.push ["https://vrvis.myget.org/F/aardvark_public/api/v2" ,"public.key"]
)

"CreatePackage" ==> "PushDev"


entry()
