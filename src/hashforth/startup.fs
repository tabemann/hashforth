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

\ Services for memory management
VARIABLE SYS-ALLOCATE
VARIABLE SYS-RESIZE
VARIABLE SYS-FREE

\ Allocate memory in the heap; -1 is returned on success and 0 is returned on
\ failure.
: ALLOCATE ( bytes -- addr -1|0 ) SYS-ALLOCATE @ SYS ;

\ Resize memory in the heap allocated with ALLOCATE or RESIZE; -1 is returned
\ on success and 0 is returned on failure.
: RESIZE ( addr new-bytes -- new-addr -1|0 ) SYS-RESIZE @ SYS ;

\ Free memory in the heap allocated with ALLOCATE or RESIZE; -1 is returned on
\ success and 0 is returned on failure.
: FREE ( addr -- -1|0 ) SYS-FREE @ SYS ;

\ Memory management failure exception
: X-MEMORY-MANAGEMENT-FAILURE ( -- )
  SPACE ." failed to allocate/free memory" CR ;

\ Allocate memory in the heap, raising an exception if allocation fails.
: ALLOCATE! ( bytes -- addr ) ALLOCATE AVERTS X-MEMORY-MANAGEMENT-FAILURE ;

\ Resize memory in the heap, raising an exception if allocation fails.
: RESIZE! ( addr new-bytes -- new-addr )
  RESIZE AVERTS X-MEMORY-MANAGEMENT-FAILURE ;

\ Free memory in the heap, raising an exception if freeing fails.
: FREE! ( addr -- ) FREE AVERTS X-MEMORY-MANAGEMENT-FAILURE ;

\ Add a value to an address
: +! ( n addr ) DUP @ ROT + SWAP ! ;

: BIN 2 BASE ! ;
: BINARY 2 BASE ! ;

: OCT 8 BASE ! ;
: OCTAL 8 BASE ! ;

: DEC 10 BASE ! ;
: DECIMAL 10 BASE ! ;

: HEX 16 BASE ! ;

\ Execute an xt with a specified BASE set, restoring BASE afterwards even if
\ an exception occurs
: BASE-EXECUTE ( i*x xt base -- j*x ) BASE @ >R BASE ! TRY R> BASE ! ?RAISE ;

\ Execute . with a specified base
: BASE. ( n base -- ) ['] . SWAP BASE-EXECUTE ;

\ Execute (.) with a specified base
: (BASE.) ( n base -- ) ['] (.) SWAP BASE-EXECUTE ;

\ Execute U. with a specified base
: UBASE. ( u base -- ) ['] U. SWAP BASE-EXECUTE ;

\ Exxecute (U.) with a specified base
: (UBASE.) ( u base -- ) ['] (U.) SWAP BASE-EXECUTE ;

: 1+ 1 + ;

: 1- 1 - ;

: 0= 0 = ;

: 0<> 0 <> ;

: 0< 0 < ;

: 0> 0 > ;

: 0<= 0 <= ;

: 0>= 0 >= ;

: ABS DUP 0< IF NEGATE THEN ;

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

: DEFER CREATE ['] ABORT , DOES> @ EXECUTE ;

: DEFER! >BODY ! ;

: DEFER@ >BODY @ ;

: ACTION-OF STATE @ IF & ['] & DEFER@ ELSE ' DEFER@ THEN ; IMMEDIATE

: IS STATE @ IF & ['] & DEFER! ELSE ' DEFER! THEN ; IMMEDIATE

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

: CASE 0 ; IMMEDIATE COMPILE-ONLY

: OF & 2DUP & = & IF & 2DROP ; IMMEDIATE COMPILE-ONLY

: ENDOF SWAP ?DUP IF HERE SWAP ! THEN
  & BRANCH HERE 0 , SWAP & ELSE & DROP & THEN ; IMMEDIATE COMPILE-ONLY

: ENDCASE & DROP ?DUP IF HERE SWAP ! THEN ; IMMEDIATE COMPILE-ONLY

: FILL ( addr count char -- )
  SWAP BEGIN DUP 0 > WHILE ROT 2 PICK OVER C! 1 + ROT ROT 1 - REPEAT
  DROP 2DROP ;

: ALIGN HERE CELL-SIZE MOD DUP 0<> IF
    CELL-SIZE SWAP - ALLOT
  ELSE
    DROP
  THEN ;

: ALIGNED DUP CELL-SIZE MOD DUP 0<> IF
    CELL-SIZE SWAP - +
  ELSE
    DROP
  THEN ;

: WALIGN HERE 4 MOD DUP 0<> IF
    4 SWAP - ALLOT
  ELSE
    DROP
  THEN ;

: WALIGNED DUP 4 MOD DUP 0<> IF
    4 SWAP - +
  ELSE
    DROP
  THEN ;

: HALIGN HERE 2 MOD DUP 0<> IF
    2 SWAP - ALLOT
  ELSE
    DROP
  THEN ;

: HALIGNED DUP 2 MOD DUP 0<> IF
    2 SWAP - +
  ELSE
    DROP
  THEN ;

: BEGIN-STRUCTURE CREATE HERE 0 0 , DOES> @ ;

: END-STRUCTURE SWAP ! ;

: +FIELD CREATE OVER , + DOES> @ + ;

: CFIELD: CREATE DUP , 1 + DOES> @ + ;

: HFIELD: CREATE DUP , 2 + DOES> @ + ;

: WFIELD: CREATE DUP , 4 + DOES> @ + ;

: FIELD: CREATE ALIGNED DUP , [ CELL-SIZE ] LITERAL + DOES> @ + ;

\ Add two times
: TIME+ ( s1 ns1 s2 ns2 -- s3 ns3 )
  ROT + DUP 1000000000 >= IF
    1000000000 - ROT ROT + 1 + SWAP
  ELSE DUP 0 < IF
    1000000000 + ROT ROT + 1 - SWAP
  ELSE
    ROT ROT + SWAP
  THEN THEN ;

\ Get a sign
: SIGN ( n -- ) DUP 0 > IF 1 ELSE 0 < IF -1 ELSE 0 THEN THEN ;

\ Turn a counted string into a normal string
: COUNT ( c-addr1 - c-addr2 u ) DUP C@ SWAP 1 + SWAP ;

\ Initialize memory services
: INIT-MEM ( -- )
  S" ALLOCATE" SYS-LOOKUP SYS-ALLOCATE !
  S" RESIZE" SYS-LOOKUP SYS-RESIZE !
  S" FREE" SYS-LOOKUP SYS-FREE ! ;

INIT-MEM
