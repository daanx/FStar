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
open Microsoft.FStar
open Microsoft.FStar.Util
open Microsoft.FStar.Getopt

type debug_level_t = 
    | Low
    | Medium
    | High
    | Extreme

let show_signatures = Util.mk_ref []
let norm_then_print = Util.mk_ref true
let z3_exe = Util.mk_ref (Platform.exe "z3")
let fvdie = Util.mk_ref false
let z3log = Util.mk_ref false
let silent=Util.mk_ref false
let debug=Util.mk_ref []
let debug_level = Util.mk_ref Low 
let dlevel = function 
    | "Low" -> Low
    | "Medium" -> Medium
    | "High" -> High
    | "Extreme" -> Extreme
    | _ -> failwith "Unrecognized debug level"
let debug_level_geq l1 l2 = match l2 with 
    | Low -> true
    | Medium -> (l1 = Medium || l1 = High || l1 = Extreme)
    | High -> (l1 = High || l1 = Extreme)
    | Extreme -> l1 = Extreme 
let log_types = Util.mk_ref false
let print_effect_args=Util.mk_ref false
let print_real_names = Util.mk_ref false
let dump_module = Util.mk_ref None
let logQueries = Util.mk_ref false
let z3exe = Util.mk_ref true
let outputDir = Util.mk_ref (Some ".")
let describe_queries = Util.mk_ref false
let skip_queries : ref<option<int>> = Util.mk_ref None
let skipped_queries = Util.mk_ref 0
let fstar_home_opt = Util.mk_ref None
let _fstar_home = Util.mk_ref ""
let prims_ref = Util.mk_ref None
let __unsafe = Util.mk_ref false
let z3timeout = Util.mk_ref 5
let pretype = Util.mk_ref true
let codegen = Util.mk_ref None
let admit_fsi = Util.mk_ref []
let trace_error = Util.mk_ref false
let verify = Util.mk_ref false
let z3_optimize_full_applications = Util.mk_ref true

let set_fstar_home () = 
  let fh = match !fstar_home_opt with 
    | None ->
      let x = Util.expand_environment_variable "FSTAR_HOME" in
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

let skip_first_queries s =
  try
    let n = int_of_string s in
    (if n = 0 then ()
     else if n > 0 then 
       (Util.print_string (Util.format1 "SKIPPING THE FIRST %s QUERIES!!!\n" (Util.string_of_int n));
        skip_queries := Some n)
     else (Util.print_string ("error: can't skip a negative number ('" ^ s ^ "') of queries\n");
           exit 1))
  with
    | _ -> 
      (Util.print_string ("error: argument '" ^ s ^ "' of --UNSAFE_skip_first_queries is not a number\n");
       exit 1)  

let display_usage specs =
  Util.print_string "fstar [option] infile...";
  List.iter
    (fun (_, flag, p, doc) ->
       match p with
         | ZeroArgs ig ->
             if doc = "" then Util.print_string (Util.format1 "  --%s\n" flag)
             else Util.print_string (Util.format2 "  --%s  %s\n" flag doc)
         | OneArg (_, argname) ->
             if doc = "" then Util.print_string (Util.format2 "  --%s %s\n" flag argname)
             else Util.print_string (Util.format3 "  --%s %s  %s\n" flag argname doc))
    specs

let specs () : list<Getopt.opt> = 
  let specs =   
    [( noshort, "trace_error", ZeroArgs (fun () -> trace_error := true), "Don't print an error message; show an exception trace instead");
     ( noshort, "codegen", OneArg ((fun s -> codegen := Some s), "OCaml|F#|JS"), "Generate code for execution");
     ( noshort, "pretype", ZeroArgs (fun () -> pretype := true), "Run the pre-type checker");
     ( noshort, "fstar_home", OneArg ((fun x -> fstar_home_opt := Some x), "dir"), "Set the FSTAR_HOME variable to dir");
     ( noshort, "silent", ZeroArgs (fun () -> silent := true), "");
     ( noshort, "prims", OneArg ((fun x -> prims_ref := Some x), "file"), "");
     ( noshort, "prn", ZeroArgs (fun () -> print_real_names := true), "Print real names---you may want to use this in conjunction with logQueries");
     ( noshort, "debug", OneArg ((fun x -> debug := x::!debug), "module name"), "Print LOTS of debugging information while checking module [arg]");
     ( noshort, "debug_level", OneArg ((fun x -> debug_level := dlevel x), "Low|Medium|High|Extreme"), "Control the verbosity of debugging info");
     ( noshort, "log_types", ZeroArgs (fun () -> log_types := true), "Print types computed for data/val/let-bindings");
     ( noshort, "print_effect_args", ZeroArgs (fun () -> print_effect_args := true), "Print inferred predicate transformers for all computation types");
     ( noshort, "dump_module", OneArg ((fun x -> dump_module := Some x), "module name"), "");
     ( noshort, "z3timeout", OneArg ((fun s -> z3timeout := int_of_string s), "t"), "Set the Z3 per-query (soft) timeout to t seconds (default 5)");
     ( noshort, "logQueries", ZeroArgs (fun () -> logQueries := true), "Log the Z3 queries in queries.smt2");
     ( noshort, "UNSAFE", ZeroArgs (fun () -> Util.print_string "UNSAFE MODE!\n"; __unsafe := true), "");
     ( noshort, "describe_queries", ZeroArgs (fun () -> describe_queries := true), "Print the queried formula and its location");
     ( noshort, "UNSAFE_skip_first_queries", OneArg ((fun x -> skip_first_queries x), "n"), "Skip the first n queries");
     ( noshort, "admit_fsi", OneArg ((fun x -> admit_fsi := x::!admit_fsi), "module name"), "Treat .fsi as a .fst");
     ( noshort, "odir", OneArg ((fun x -> outputDir := Some x), "dir"), "Place output in directory dir");
     ( noshort, "verify", ZeroArgs (fun () -> verify := true), "Call the SMT solver to discharge verifications conditions");
     ( noshort, "z3exe", OneArg ((fun x -> z3_exe := x), "path"), "Path to the Z3 SMT solver");
     ( noshort, "print_before_norm", ZeroArgs(fun () -> norm_then_print := false), "Do not normalize types before printing (for debugging)");
     ( noshort, "show_signatures", OneArg((fun x -> show_signatures := x::!show_signatures), "module name"), "Show the checked signatures for all top-level symbols in the module");
     ( noshort, "optimize_full_applications", OneArg((fun a -> if a="true" then z3_optimize_full_applications := true else z3_optimize_full_applications := false), "true|false"), 
                        "Generate shallow embeddings of function symbols")
     ] in 
     ( 'h', "help", ZeroArgs (fun x -> display_usage specs; exit 0), "Display this information")::specs
