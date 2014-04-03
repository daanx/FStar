(*
   Copyright 2008-2014 Nikhil Swamy and Microsoft Research

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*)
#light "off"

// (c) Microsoft Corporation. All rights reserved
module Microsoft.FStar.Options
open Getopt
open Util

let z3log = ref false
let quiet = ref true
let silent=ref false
let print_real_names = ref true
let dump_module = ref None
let logQueries = ref false
let z3exe = ref true
let outputDir = ref (Some ".")
let describe_queries = ref false
let skip_queries = ref (None : Option<int>)
let skipped_queries = ref 0
let fstar_home_opt = ref None
let _fstar_home = ref ""
let prims_ref = ref None
let __unsafe = ref false
let z3timeout = ref None

let query_file () = 
  let f = "query-" ^ (Util.string_of_int <| Util.query_count()) ^ ".smt2" in
  match !outputDir with 
    | Some x -> x ^ "/"^ f
    | None -> f

let set_fstar_home () = 
  let fh = match !fstar_home_opt with 
    | None ->
      let x = System.Environment.ExpandEnvironmentVariables("%FSTAR_HOME%") in
      _fstar_home := x;
      fstar_home_opt := Some x;
      x
    | Some x -> _fstar_home := x; x in
  fh
let get_fstar_home () = match !fstar_home_opt with 
    | None -> ignore <| set_fstar_home(); !_fstar_home
    | Some x -> x

let prims () = match !prims_ref with 
  | None -> (get_fstar_home()) ^ "/lib/prims.fst" 
  | Some x -> x

let prependOutputDir fname = match !outputDir with
  | None -> fname
  | Some x -> x ^ "/" ^ fname 

let getZ3Timeout () = match !z3timeout with 
  | Some s -> s
  | _ -> "10000"

let skip_first_queries s =
  try
    let n = int_of_string s in
      if n < 0 then
        (System.Console.Error.Write("error: can't skip a negative number ('" + s + "') of queries\n");
         System.Environment.Exit(1))
      else
        (if n = 0 then ()
         else
           (Printf.printf "SKIPPING THE FIRST %d QUERIES!!!\n" n;
            skip_queries := Some n))
  with
    | _ -> 
      (System.Console.Error.Write("error: argument '" + s + "' of --UNSAFE_skip_first_queries is not a number\n");
       System.Environment.Exit(1))  

let rec specs : list<Getopt.opt> = 
[
  ( 'h', "help", ZeroArgs (fun x -> display_usage (); System.Environment.Exit(0)), "Display this information");
  ( noshort, "z3exe", ZeroArgs (fun () -> logQueries := true; z3exe := true), "Call z3.exe instead of via the .NET API (implies --logQueries)");
  ( noshort, "fstar_home", OneArg ((fun x -> fstar_home_opt := Some x), "dir"), "Set the FSTAR_HOME variable to dir");
  ( noshort, "profile", ZeroArgs (fun () -> Printf.printf "Setting profiling flag!\n"; Profiling.profiling := true), "");
  ( noshort, "silent", ZeroArgs (fun () -> silent := true), "");
  ( noshort, "prims", OneArg ((fun x -> prims_ref := Some x), "file"), "");
  ( noshort, "prn", ZeroArgs (fun () -> print_real_names := true), "Print real names---you may want to use this in conjunction with logQueries");
  ( noshort, "dump_module", OneArg ((fun x -> dump_module := Some x), "module name"), "");
  ( noshort, "z3timeout", OneArg ((fun s -> z3timeout := Some s), "t"), "Set the Z3 soft timeout to t milliseconds");
  ( noshort, "logQueries", ZeroArgs (fun () -> logQueries := true;), "Log the Z3 queries in $FSTAR_HOME/bin/queries/, or in odir, if set; also see --prn");
  ( noshort, "UNSAFE", ZeroArgs (fun () -> Printf.printf "UNSAFE MODE!\n"; __unsafe := true), "");
  ( noshort, "describe_queries", ZeroArgs (fun () -> describe_queries := true), "Print the queried formula and its location");
  ( noshort, "UNSAFE_skip_first_queries", OneArg ((fun x -> skip_first_queries x), "n"), "Skip the first n queries");
  ( noshort, "odir", OneArg ((fun x -> outputDir := Some x), "dir"), "Place output in directory dir");
]
and display_usage () =
  printfn "fstar [option] infile...";
  List.iter
    (fun (_, flag, p, doc) ->
       match p with
         | ZeroArgs _ ->
             if doc = "" then printfn "  --%s" flag
             else printfn "  --%s  %s" flag doc
         | OneArg (_, argname) ->
             if doc = "" then printfn "  --%s %s" flag argname
             else printfn "  --%s %s  %s" flag argname doc)
    specs