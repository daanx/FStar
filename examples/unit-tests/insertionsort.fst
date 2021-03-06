(*
   Copyright 2008-2014 Nikhil Swamy, Chantal Keller and Microsoft Research

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


module InsertionSort
open Prims.PURE

(* Check that a list is sorted *)
val sorted: list int -> Tot bool
let rec sorted l = match l with
    | [] -> true
    | [x] -> true
    | x::y::xs -> x <= y && sorted (y::xs)

val test_sorted: x:int -> l:list int -> Fact unit ((sorted (x::l) /\ is_Cons l) ==> x <= Cons.hd l)
let test_sorted x l = ()

val test_sorted2: unit -> Tot (m:list int{sorted m})
let test_sorted2 () = Nil

(* Fact about sorted *)
val mem: 'a -> list 'a -> Tot bool
let rec mem a l = match l with
  | [] -> false
  | hd::tl -> hd=a || mem a tl

val sorted_smaller: x:int -> y:int -> l:list int ->
                    Fact unit (sorted (x::l) /\ mem y l ==> x <= y)
let rec sorted_smaller x y l = match l with
    | [] -> ()
    | z::zs -> if z=y then () else sorted_smaller x y zs

val insert : i:int -> l:list int{sorted l} -> Tot (m:list int{sorted m /\ (forall x. mem x m <==> x==i \/ mem x l)})
let rec insert i l = match l with
  | [] -> [i]
  | hd::tl ->
     if i <= hd
     then i::l
     else let i_tl = insert i tl in 
          let (z::_) = i_tl in
          sorted_smaller hd z tl; (* need to call the lemma explicitly, currently *)
          hd::i_tl

(* Insertion sort *)
val sort : l:list int -> Tot (m:list int{sorted m /\ (forall x. mem x l == mem x m)})
let rec sort l = match l with
    | [] -> []
    | x::xs -> insert x (sort xs)
