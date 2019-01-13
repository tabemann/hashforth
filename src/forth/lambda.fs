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

GET-ORDER GET-CURRENT BASE @

DECIMAL
FORTh-WORDLIST 1 SET-ORDER
FORTH-WORDlIST SET-CURRENT

WORDLIST CONSTANT LAMBDA-WORDLIST

FORTH-WORDLIST LAMBDA-WORDLIST 2 SET-ORDER
LAMBDA-WORDLIST SET-CURRENT

: [: ( -- xt) ( in xt: -- )
  & BRANCH HERE 0 , 0 COMPILE, LATESTXT :NONAME ; IMMEDIATE

: ;] ( -- ) ( in xt: -- )
  & EXIT 0 COMPILE, ROT HERE SWAP ! & (LIT) , LATESTXT-VALUE ! ; IMMEDIATE

: OPTION ( ... flag true-xt -- ... )
  STATE @ IF
    & SWAP & IF & EXECUTE & ELSE & DROP & THEN
  ELSE
    SWAP IF EXECUTE ELSE DROP THEN
  THEN ; IMMEDIATE

: CHOOSE ( ... flag true-xt false-xt -- ... )
  STATE @ IF
    & ROT & IF & DROP & EXECUTE & ELSE & NIP & EXECUTE & THEN
  ELSE
    ROT IF DROP EXECUTE ELSE NIP EXECUTE THEN
  THEN ; IMMEDIATE

: LOOP-UNTIL ( ... xt -- ... )
  STATE @ IF
    & >R & BEGIN & R@ & EXECUTE & UNTIL & R> & DROP
  ELSE
    >R BEGIN R@ EXECUTE UNTIL R> DROP
  THEN ; IMMEDIATE

: WHILE-LOOP ( ... while-xt body-xt -- ... )
  STATE @ IF
    & 2>R & BEGIN & 2R@ & DROP & EXECUTE & WHILE & R@ & EXECUTE & REPEAT
    & 2R> & 2DROP
  ELSE
    2>R BEGIN 2R@ DROP EXECUTE WHILE R@ EXECUTE REPEAT 2R> 2DROP
  THEN ; IMMEDIATE

: COUNT-LOOP ( ... limit init xt -- ... )
  STATE @ IF
    & ROT & ROT & ?DO & I & SWAP & DUP & >R & EXECUTE & R> & LOOP & DROP
  ELSE
    ROT ROT ?DO I SWAP DUP >R EXECUTE R> LOOP DROP
  THEN ; IMMEDIATE

: COUNT+LOOP ( ... limit init xt -- ... )
  STATE @ IF
    & ROT & ROT & ?DO & I & SWAP & DUP & >R & EXECUTE & R> & SWAP & +LOOP & DROP
  ELSE
    ROT ROT ?DO I SWAP DUP >R EXECUTE R> SWAP +LOOP DROP
  THEN ; IMMEDIATE

: FETCH-ADVANCE ( a-addr1 count1 -- a-addr2 count2 x )
  & SWAP & DUP & @ & ROT & 1- & ROT & CELL+ & SWAP & ROT ; IMMEDIATE

: HIDE-3-BELOW-2 ( x1 x2 x3 x4 x5 -- x4 x5 ) ( R: -- x3 x2 x1 )
  & ROT & >R & ROT & >R & ROT & >R ; IMMEDIATE

: SHOW-3 ( -- x1 x2 x3 ) ( R: x3 x2 x1 -- ) & R> & R> & R> ; IMMEDIATE

: ITER ( a-addr count xt -- )
  [: OVER 0> ;] [: ROT ROT FETCH-ADVANCE 3 PICK HIDE-3-BELOW-2 EXECUTE
     SHOW-3 ROT ;] WHILE-LOOP
  2DROP DROP ;

: HIDE-4-BELOW-3 ( x1 x2 x3 x4 x5 x6 x7 -- x5 x6 x7 ) ( R: -- x4 x3 x2 x1 )
  & (LIT) 3 , & ROLL & >R & (LIT) 3 , & ROLL & >R
  & (LIT) 3 , & ROLL & >R & (LIT) 3 , & ROLL & >R ; IMMEDIATE

: SHOW-4 ( -- x1 x2 x3 x4 ) ( R: x4 x3 x2 x1 -- )
  & R> & R> & R> & R> ; IMMEDIATE

: ITERI ( a-addr count xt -- )
  0
  [: 2 PICK 0> ;]
  [: 3 ROLL 3 ROLL FETCH-ADVANCE 3 PICK SWAP 5 PICK HIDE-4-BELOW-3 EXECUTE
     SHOW-4 3 ROLL 3 ROLL 1+ ;] WHILE-LOOP
  2DROP 2DROP ;

: HIDE-4-BELOW-2 ( x1 x2 x3 x4 x5 x6 -- x5 x6 ) ( R: -- x4 x3 x2 x1 )
  & ROT & >R & ROT & >R & ROT & >R & ROT & >R ; IMMEDIATE

: SHOW-4-BELOW-1 ( x5 -- x1 x2 x3 x4 x5 ) ( R: x4 x3 x2 x1 -- )
  & R> & R> & R> & R> & (LIT) 4 , & ROLL ; IMMEDIATE

: (MAP) ( a-addr1 count1 a-addr2 xt -- count2 )
  [: 2 PICK 0> ;]
  [: 3 ROLL 3 ROLL FETCH-ADVANCE 3 PICK HIDE-4-BELOW-2 EXECUTE
     SHOW-4-BELOW-1 4 ROLL TUCK ! CELL+ 3 ROLL ;] WHILE-LOOP
  2DROP 2DROP ;

: MAP ( a-addr1 count1 a-addr2 xt -- a-addr2 count2 )
  ROT ROT 2>R 2R@ ROT (MAP) 2R> SWAP ;

: HIDE-6-BELOW-2 ( x1 x2 x3 x4 x5 x6 x7 x8 -- x7 x8 )
  ( R: -- x6 x5 x4 x3 x2 x1 )
  & ROT & >R & ROT & >R & ROT & >R & ROT & >R & ROT & >R & ROT & >R ; IMMEDIATE

: SHOW-6-BELOW-1 ( x7 -- x1 x2 x3 x4 x5 x6 x7 ) ( R: x6 x5 x4 x3 x2 x1 -- )
  & R> & R> & R> & R> & R> & R> & (LIT) 6 , & ROLL ; IMMEDIATE

: (FILTER) ( a-addr1 count1 a-addr2 xt -- count2 )
  0
  [: 3 PICK 0> ;]
  [: 4 ROLL 4 ROLL FETCH-ADVANCE DUP 5 PICK HIDE-6-BELOW-2 EXECUTE
     SHOW-6-BELOW-1
     [: ( a2 xt c2 a1 c1 x ) 5 ROLL TUCK ! CELL+ 3 ROLL 1+ 4 ROLL SWAP ;]
     [: DROP 4 ROLL 4 ROLL 4 ROLL ;] CHOOSE ;] WHILE-LOOP
  ROT ROT 2DROP ROT ROT 2DROP ;

: FILTER ( a-addr1 count1 a-addr2 xt -- a-addr2 count2 )
  SWAP >R R@ SWAP (FILTER) R> SWAP ;

: HIDE-7-BELOW-2 ( x1 x2 x3 x4 x5 x6 x7 x8 x9 -- x8 x9 )
  ( R: -- x7 x6 x5 x4 x3 x2 x1 )
  & ROT & >R & ROT & >R & ROT & >R & ROT & >R & ROT & >R & ROT & >R
  & ROT & >R ; IMMEDIATE

: SHOW-7-BELOW-1 ( x8 -- x1 x2 x3 x4 x5 x6 x7 x8 )
  ( R: x7 x6 x5 x4 x3 x2 x1 -- )
  & R> & R> & R> & R> & R> & R> & R> & (LIT) 7 , & ROLL ; IMMEDIATE

: SHOW-6-BELOW-1 ( x7 -- x1 x2 x3 x4 x5 x6 x7 ) ( R: x6 x5 x4 x3 x2 x1 -- )
  & R> & R> & R> & R> & R> & R> & (LIT) 6 , & ROLL ; IMMEDIATE

: (FILTER-MAP) ( a-addr1 count1 a-addr2 xt-filter xt-map -- count2 )
  0
  [: 4 PICK 0> ;]
  [: 5 ROLL 5 ROLL FETCH-ADVANCE DUP 6 PICK HIDE-7-BELOW-2 EXECUTE
     SHOW-7-BELOW-1
     [: 4 PICK HIDE-6-BELOW-2 EXECUTE SHOW-6-BELOW-1 6 ROLL TUCK !
        CELL+ 3 ROLL 1+ 5 ROLL 5 ROLL ROT ;]
     [: DROP 5 ROLL 5 ROLL 5 ROLL 5 ROLL ;] CHOOSE ;] WHILE-LOOP
  ROT ROT 2DROP ROT ROT 2DROP NIP ;

: FILTER-MAP ( a-addr1 count1 a-addr2 xt-filter xt-map -- a-addr2 count2 )
  ROT >R R@ ROT ROT (FILTER-MAP) R> SWAP ;

BASE ! SET-CURRENT SET-ORDER
