\ Copyright (c) 2019, Travis Bemann
\ All rights reserved.
\ 
\ Redistribution and use in source and binary forms, with or without
\ modification, are permitted provided that the following conditions are met:
\ 
\ 1. Redistributions of source code must retain the above copyright notice,
\    this list of conditions and the following disclaimer.
\ 
\ 2. Redistributions in binary form must reproduce the above copyright notice,
\    this list of conditions and the following disclaimer in the documentation
\    and/or other materials provided with the distribution.
\ 
\ 3. Neither the name of the copyright holder nor the names of its
\    contributors may be used to endorse or promote products derived from
\    this software without specific prior written permission.
\ 
\ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
\ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
\ IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
\ ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
\ LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
\ CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
\ SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
\ INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
\ CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
\ ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
\ POSSIBILITY OF SUCH DAMAGE.

: 1+ 1 + ;

: 1- 1 - ;

: 0= 0 = ;

: 0<> 0 <> ;

: 0< 0 < ;

: 0> 0 > ;

: 0<= 0 <= ;

: 0>= 0 >= ;

: POSTPONE ' DUP IMMEDIATE? IF
    COMPILE,
  ELSE
    COMPILE (LIT) , COMPILE COMPILE,
  THEN ; IMMEDIATE COMPILE-ONLY

: & POSTPONE POSTPONE ; IMMEDIATE

: 2SWAP 3 ROLL 3 ROLL ;

: 2OVER 3 PICK 3 PICK ;

: 2ROT 5 ROLL 5 ROLL ;

: 2! TUCK ! CELL+ ! ;

: 2@ DUP @ SWAP CELL+ @ SWAP ;

: 2, HERE 2! [ 2 CELLS ] LITERAL ALLOT ;

: 2>R R> ROT ROT SWAP >R >R >R ;

: 2R> R> R> R> SWAP ROT >R ;

: 2R@ R> R> R> 2DUP SWAP ROT >R ROT >R ROT >R ;

: 2VARIABLE CREATE 0 0 2, ;

: 2CONSTANT CREATE 2, DOES> 2@ ;

: DO & (LIT) HERE DUP 0 , & >R & 2>R HERE ; IMMEDIATE COMPILE-ONLY

: ?DO & (LIT) HERE 0 , & >R & 2DUP & 2>R & <> & 0BRANCH HERE 0 , HERE
  ; IMMEDIATE COMPILE-ONLY

: LOOP & 2R> & 1+ & 2DUP & 2>R & = & 0BRANCH , HERE SWAP ! HERE SWAP !
  & 2R> & 2DROP & R> & DROP ; IMMEDIATE COMPILE-ONLY

: +LOOP & 2R> & ROT & DUP & 0>= & IF
    & SWAP & DUP & (LIT) 3 , & PICK & < & ROT & ROT & +
    & DUP & (LIT) 3 , & PICK & >= & ROT & AND & ROT & ROT & 2>R
  & ELSE
    & SWAP & DUP & (LIT) 3 , & PICK & >= & ROT & ROT & +
    & DUP & (LIT) 3 , & PICK & < & ROT & AND & ROT & ROT & 2>R
  & THEN
  & 0BRANCH , HERE SWAP ! HERE SWAP ! & 2R> & 2DROP & R> & DROP ;
  IMMEDIATE COMPILE-ONLY

: LEAVE R> 2R> 2DROP >R ;

: UNLOOP R> 2R> 2DROP R> DROP >R ;

: I R> R@ SWAP >R ;

: J 2R> 2R> R@ 4 ROLL 4 ROLL 4 ROLL 4 ROLL 2>R 2>R ;
