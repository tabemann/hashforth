\ Copyright (c) 2018-2019, Travis Bemann
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
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT VECTOR-WORDLIST
FORTH-WORDLIST VECTOR-WORDLIST 2 SET-ORDER
VECTOR-WORDLIST SET-CURRENT

WORDLIST CONSTANT VECTOR-PRIVATE-WORDLIST
FORTH-WORDLIST VECTOR-PRIVATE-WORDLIST VECTOR-WORDLIST
LAMBDA-WORDLIST 4 SET-ORDER
VECTOR-PRIVATE-WORDLIST SET-CURRENT

\ The vector structure
BEGIN-STRUCTURE VECTOR-SIZE
  \ The vector flags
  FIELD: VECTOR-FLAGS
  
  \ The vector circular buffer
  FIELD: VECTOR-DATA

  \ The number of entries in the vector
  FIELD: VECTOR-COUNT

  \ The maximum number of entries in the vector (without resizing)
  FIELD: VECTOR-MAX-COUNT

  \ The size of an entry in the vector
  FIELD: VECTOR-ENTRY-SIZE

  \ The current start index in the circular buffer
  FIELD: VECTOR-START-INDEX

  \ The current end index in the circular buffer
  FIELD: VECTOR-END-INDEX

  \ The finalizer word
  FIELD: VECTOR-FINALIZER

  \ THe extra finalizer argument
  FIELD: VECTOR-FINALIZER-ARG
END-STRUCTURE

\ The vector-is-allocated flag
1 CONSTANT ALLOCATED-VECTOR

\ Internal - get address of a block at an index in a vector.
: GET-VECTOR-ENTRY ( index vector -- addr )
  2DUP VECTOR-START-INDEX @ SWAP <= IF
    DUP VECTOR-START-INDEX @ ROT 1 + - OVER VECTOR-MAX-COUNT @ +
  ELSE
    DUP VECTOR-START-INDEX @ ROT 1 + -
  THEN
  OVER VECTOR-ENTRY-SIZE @ * SWAP VECTOR-DATA @ + ;

\ Carry out finalizing of an entry if there is a finalizer xt
: DO-FINALIZE ( index vector -- )
  DUP VECTOR-FINALIZER @ IF
    TUCK GET-VECTOR-ENTRY OVER VECTOR-FINALIZER-ARG @ ROT
    VECTOR-FINALIZER @ EXECUTE
  ELSE
    2DROP
  THEN ;

\ Internal - wrap an index backward
: WRAP-BACK ( index vector -- index )
  SWAP 1- DUP 0 < IF SWAP VECTOR-MAX-COUNT @ + ELSE NIP THEN ;

\ Internal - get a block at an index in a vector.
: GET-VECTOR ( addr index vector -- )
  2DUP VECTOR-START-INDEX @ SWAP <= IF
    DUP VECTOR-START-INDEX @ ROT 1 + - OVER VECTOR-MAX-COUNT @ +
  ELSE
    DUP VECTOR-START-INDEX @ ROT 1 + -
  THEN
  OVER VECTOR-ENTRY-SIZE @ * OVER VECTOR-DATA @ +
  ROT ROT VECTOR-ENTRY-SIZE @ CMOVE ;

\ Internal - set a block at an index in a vector.
: SET-VECTOR ( addr index vector -- )
  2DUP VECTOR-START-INDEX @ SWAP < IF
    DUP VECTOR-START-INDEX @ ROT 1 + - OVER VECTOR-MAX-COUNT @ +
  ELSE
    DUP VECTOR-START-INDEX @ ROT 1 + -
  THEN
  OVER VECTOR-ENTRY-SIZE @ * OVER VECTOR-DATA @ +
  ROT SWAP ROT VECTOR-ENTRY-SIZE @ CMOVE ;

\ Internal - push a block onto the start of a vector.
: PUSH-START-VECTOR ( addr vector -- )
  TUCK VECTOR-DATA @ 2 PICK VECTOR-START-INDEX @
  3 PICK VECTOR-ENTRY-SIZE @ * + 2 PICK VECTOR-ENTRY-SIZE @ CMOVE
  DUP VECTOR-COUNT @ 1+ OVER VECTOR-COUNT !
  DUP VECTOR-START-INDEX @ 1+ OVER VECTOR-MAX-COUNT @ MOD
  SWAP VECTOR-START-INDEX ! ;

\ Internal - pop a block from the start of a vector.
: POP-START-VECTOR ( addr vector -- )
  DUP VECTOR-START-INDEX @ OVER WRAP-BACK OVER VECTOR-START-INDEX !
  DUP VECTOR-DATA @ OVER VECTOR-START-INDEX @
  2 PICK VECTOR-ENTRY-SIZE @ * + ROT 2 PICK VECTOR-ENTRY-SIZE @ CMOVE
  DUP VECTOR-COUNT @ 1- SWAP VECTOR-COUNT ! ;

\ Internal - drop a block from the start of a vector.
: DROP-START-VECTOR ( vector -- )
  DUP VECTOR-START-INDEX @ OVER WRAP-BACK OVER VECTOR-START-INDEX !
  DUP VECTOR-COUNT @ 1- SWAP VECTOR-COUNT ! ;

\ Internal - peek a block from the start a vector.
: PEEK-START-VECTOR ( addr vector -- )
  DUP VECTOR-DATA @ OVER VECTOR-START-INDEX @ 2 PICK WRAP-BACK
  2 PICK VECTOR-ENTRY-SIZE @ * + ROT ROT VECTOR-ENTRY-SIZE @ CMOVE ;

\ Internal - push a block onto the start of a vector.
: PUSH-END-VECTOR ( addr vector -- )
  DUP VECTOR-END-INDEX @ OVER WRAP-BACK OVER VECTOR-END-INDEX !
  TUCK VECTOR-DATA @ 2 PICK VECTOR-END-INDEX @
  3 PICK VECTOR-ENTRY-SIZE @ * + 2 PICK VECTOR-ENTRY-SIZE @ CMOVE
  DUP VECTOR-COUNT @ 1+ SWAP VECTOR-COUNT ! ;

\ Internal - pop a block from the end of a vector.
: POP-END-VECTOR ( addr vector -- )
  DUP VECTOR-DATA @ OVER VECTOR-END-INDEX @
  2 PICK VECTOR-ENTRY-SIZE @ * + ROT 2 PICK VECTOR-ENTRY-SIZE @ CMOVE
  DUP VECTOR-COUNT @ 1- OVER VECTOR-COUNT !
  DUP VECTOR-END-INDEX @ 1+ OVER VECTOR-MAX-COUNT @ MOD
  SWAP VECTOR-END-INDEX ! ;

\ Internal - drop a block from the end of a vector.
: DROP-END-VECTOR ( vector -- )
  DUP VECTOR-COUNT @ 1- OVER VECTOR-COUNT !
  DUP VECTOR-END-INDEX @ 1+ OVER VECTOR-MAX-COUNT @ MOD
  SWAP VECTOR-END-INDEX ! ;

\ Internal - peek a block from the end a vector.
: PEEK-END-VECTOR ( addr vector -- )
  DUP VECTOR-DATA @ OVER VECTOR-END-INDEX @
  2 PICK VECTOR-ENTRY-SIZE @ * + ROT ROT VECTOR-ENTRY-SIZE @ CMOVE ;

\ Prepend a vector to another vector
: PREPEND-VECTOR ( source-vector dest-vector -- )
  OVER VECTOR-ENTRY-SIZE @ HERE SWAP ALLOT
  2 PICK VECTOR-COUNT @ 0 ?DO
    DUP 3 PICK VECTOR-COUNT @ 1 - I - 4 PICK GET-VECTOR
    DUP 2 PICK PUSH-START-VECTOR
  LOOP
  OVER VECTOR-ENTRY-SIZE @ NEGATE ALLOT
  2DROP DROP ;

\ Append a vector to another vector
: APPEND-VECTOR ( source-vector dest-vector -- )
  OVER VECTOR-ENTRY-SIZE @ HERE SWAP ALLOT
  2 PICK VECTOR-COUNT @ 0 ?DO
    DUP I 4 PICK GET-VECTOR
    DUP 2 PICK PUSH-END-VECTOR
  LOOP
  OVER VECTOR-ENTRY-SIZE @ NEGATE ALLOT
  2DROP DROP ;
  
\ Expand a vector
: EXPAND-VECTOR ( vector -- success )
  HERE VECTOR-SIZE ALLOT
  0 OVER VECTOR-FLAGS !
  0 OVER VECTOR-COUNT !
  OVER VECTOR-MAX-COUNT @ 2 * OVER VECTOR-MAX-COUNT !
  OVER VECTOR-ENTRY-SIZE @ OVER VECTOR-ENTRY-SIZE !
  0 OVER VECTOR-START-INDEX !
  0 OVER VECTOR-END-INDEX !
  0 OVER VECTOR-FINALIZER !
  0 OVER VECTOR-FINALIZER-ARG !
  DUP VECTOR-MAX-COUNT @ OVER VECTOR-ENTRY-SIZE @ * ALLOCATE IF
    OVER VECTOR-DATA !
    2DUP APPEND-VECTOR
    DUP VECTOR-START-INDEX @ 2 PICK VECTOR-START-INDEX !
    DUP VECTOR-END-INDEX @ 2 PICK VECTOR-END-INDEX !
    DUP VECTOR-MAX-COUNT @ 2 PICK VECTOR-MAX-COUNT !
    OVER VECTOR-DATA @ FREE! VECTOR-DATA @ SWAP VECTOR-DATA !
    VECTOR-SIZE NEGATE ALLOT TRUE
  ELSE
    2DROP DROP VECTOR-SIZE NEGATE ALLOT FALSE
  THEN ;

\ Expand a vector to fit multiple entries
: EXPAND-MULTIPLE ( count vector -- success )
  DUP VECTOR-MAX-COUNT @ 2 PICK < IF
    DUP VECTOR-FLAGS @ ALLOCATED-VECTOR AND IF
      HERE VECTOR-SIZE ALLOT
      0 OVER VECTOR-FLAGS !
      0 OVER VECTOR-COUNT !
      ROT 2 PICK VECTOR-MAX-COUNT @ 2 * MAX OVER VECTOR-MAX-COUNT !
      OVER VECTOR-ENTRY-SIZE @ OVER VECTOR-ENTRY-SIZE !
      0 OVER VECTOR-START-INDEX !
      0 OVER VECTOR-END-INDEX !
      0 OVER VECTOR-FINALIZER !
      0 OVER VECTOR-FINALIZER-ARG !
      DUP VECTOR-MAX-COUNT @ OVER VECTOR-ENTRY-SIZE @ * ALLOCATE IF
	OVER VECTOR-DATA !
	2DUP APPEND-VECTOR
	DUP VECTOR-START-INDEX @ 2 PICK VECTOR-START-INDEX !
	DUP VECTOR-END-INDEX @ 2 PICK VECTOR-END-INDEX !
	DUP VECTOR-MAX-COUNT @ 2 PICK VECTOR-MAX-COUNT !
	OVER VECTOR-DATA @ FREE! VECTOR-DATA @ SWAP VECTOR-DATA !
	VECTOR-SIZE NEGATE ALLOT TRUE
      ELSE
	2DROP DROP VECTOR-SIZE NEGATE ALLOT FALSE
      THEN
    ELSE
      2DROP FALSE
    THEN
  ELSE
    2DROP TRUE
  THEN ;

VECTOR-WORDLIST SET-CURRENT

\ Print out the internal values of a vector.
: VECTOR. ( vector -- )
  CR ." VECTOR-DATA: " DUP VECTOR-DATA @ .
  CR ." VECTOR-FLAGS: " DUP VECTOR-FLAGS @ .
  CR ." VECTOR-COUNT: " DUP VECTOR-COUNT @ .
  CR ." VECTOR-MAX-COUNT: " DUP VECTOR-MAX-COUNT @ .
  CR ." VECTOR-ENTRY-SIZE: " DUP VECTOR-ENTRY-SIZE @ .
  CR ." VECTOR-START-INDEX: " DUP VECTOR-START-INDEX @ .
  CR ." VECTOR-END-INDEX: " VECTOR-END-INDEX @ .
  CR ." VECTOR-FINALIZER: " VECTOR-FINALIZER @ .
  CR ." VECTOR-FINALIZER-ARG: " VECTOR-FINALIZER-ARG @ . CR ;

\ Vector is not allocated exception
: X-VECTOR-NOT-ALLOCATED ( -- ) SPACE ." vector is not allocated" CR ;

\ Allot a new vector with the specified entry count and entry size.
: ALLOT-VECTOR ( entry-count entry-size -- vector )
  HERE VECTOR-SIZE ALLOT
  2 PICK OVER VECTOR-MAX-COUNT !
  0 OVER VECTOR-FLAGS !
  0 OVER VECTOR-COUNT !
  0 OVER VECTOR-START-INDEX !
  0 OVER VECTOR-END-INDEX !
  0 OVER VECTOR-FINALIZER !
  0 OVER VECTOR-FINALIZER-ARG !
  HERE 3 ROLL 3 PICK * ALLOT OVER VECTOR-DATA !
  TUCK VECTOR-ENTRY-SIZE ! ;

\ Allocate a new vector with the specified entry count and entry size.
: ALLOCATE-VECTOR ( entry-count entry-size -- vector )
  VECTOR-SIZE ALLOCATE!
  2 PICK OVER VECTOR-MAX-COUNT !
  ALLOCATED-VECTOR OVER VECTOR-FLAGS !
  0 OVER VECTOR-COUNT !
  0 OVER VECTOR-START-INDEX !
  0 OVER VECTOR-END-INDEX !
  0 OVER VECTOR-FINALIZER !
  0 OVER VECTOR-FINALIZER-ARG !
  ROT 2 PICK * ALLOCATE! OVER VECTOR-DATA !
  TUCK VECTOR-ENTRY-SIZE ! ;

\ Set a finalizer
: SET-VECTOR-FINALIZER ( finalizer finalizer-arg map -- )
  TUCK VECTOR-FINALIZER-ARG ! VECTOR-FINALIZER ! ;

\ Destroy an allocated vector.
: DESTROY-VECTOR ( vector -- )
  DUP VECTOR-FLAGS @ ALLOCATED-VECTOR = AVERTS X-VECTOR-NOT-ALLOCATED
  DUP VECTOR-DATA @ FREE! FREE! ;

\ Clear a vector.
: CLEAR-VECTOR ( vector -- )
  0 OVER VECTOR-START-INDEX ! 0 OVER VECTOR-END-INDEX ! 0 SWAP VECTOR-COUNT ! ;

\ Get the number of blocks queued in a vector.
: COUNT-VECTOR ( vector -- u ) VECTOR-COUNT @ ;

\ Get whether a vector is empty.
: EMPTY-VECTOR? ( vector -- empty ) VECTOR-COUNT @ 0 = ;

\ Get whether a vector is full.
: FULL-VECTOR? ( vector -- full )
  DUP VECTOR-COUNT @ SWAP VECTOR-MAX-COUNT @ = ;

\ Vector is not composed of cells exception
: X-NON-CELL-VECTOR ( -- ) SPACE ." non-cell vector" CR ;

\ Vector is not composed of double cells exception
: X-NON-2CELL-VECTOR ( -- ) SPACE ." non-double cell vector" CR ;

\ Evaluate an xt for the address of each member of a vector from left to right;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: ITER-LEFT-VECTOR ( xt vector -- )
  0 BEGIN
    DUP 2 PICK COUNT-VECTOR < IF
      2DUP SWAP GET-VECTOR-ENTRY SWAP >R SWAP >R SWAP >R R@ EXECUTE R> R> R> 1 +
      FALSE
    ELSE
      DROP 2DROP TRUE
    THEN
  UNTIL ;

\ Evaluate an xt for the address of each member of a vector from right to left;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: ITER-RIGHT-VECTOR ( xt vector -- )
  DUP COUNT-VECTOR 1 - BEGIN
    DUP 0 >= IF
      2DUP SWAP GET-VECTOR-ENTRY SWAP >R SWAP >R SWAP >R R@ EXECUTE R> R> R> 1 -
      FALSE
    ELSE
      DROP 2DROP TRUE
    THEN
  UNTIL ;

\ Evaluate an xt for the cell of each member of a vector from left to right;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outstack
: ITER-LEFT-VECTOR-CELL ( xt vector -- )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  [: @ SWAP DUP >R EXECUTE R> ;] SWAP ITER-LEFT-VECTOR ;

\ Evaluate an xt for the cell of each member of a vector from right to left;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outstack
: ITER-RIGHT-VECTOR-CELL ( xt vector -- )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  [: @ SWAP DUP >R EXECUTE R> ;] SWAP ITER-RIGHT-VECTOR ;

\ Evaluate an xt for the double cell of each member of a vector from left to
\ right; note that the internal state is hidden from the xt, so the xt can
\ transparently access the outstack
: ITER-LEFT-VECTOR-2CELL ( xt vector -- )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  [: 2@ ROT DUP >R EXECUTE R> ;] SWAP ITER-LEFT-VECTOR ;

\ Evaluate an xt for the double cell of each member of a vector from right to
\ left; note that the internal state is hidden from the xt, so the xt can
\ transparently access the outstack
: ITER-RIGHT-VECTOR-2CELL ( xt vector -- )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  [: 2@ ROT DUP >R EXECUTE R> ;] SWAP ITER-RIGHT-VECTOR ;

\ Get a block at an index in a vector and return whether it was successful.
: GET-VECTOR ( addr index vector -- success )
  2DUP COUNT-VECTOR < IF GET-VECTOR TRUE ELSE 2DROP DROP FALSE THEN ;

\ Get a cell at an index in a vector and return whether it was successful.
: GET-VECTOR-CELL ( index vector -- value found )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT DUP >R ROT ROT GET-VECTOR IF
    R> @ TRUE
  ELSE
    R> DROP 0 FALSE
  THEN
  -1 CELLS ALLOT ;

\ Get a double cell at an index in a vector and return whether it was
\ successful.
: GET-VECTOR-2CELL ( index vector -- value1 value2 found )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  HERE 2 CELLS ALLOT DUP >R ROT ROT GET-VECTOR IF
    R> 2@ TRUE
  ELSE
    R> DROP 0 0 FALSE
  THEN
  -2 CELLS ALLOT ;

\ Set a block at an index in a vector and return whether it was successful.
: SET-VECTOR ( addr index vector -- success )
  2DUP COUNT-VECTOR < IF
    2DUP DO-FINALIZE SET-VECTOR TRUE
  ELSE
    2DROP DROP FALSE
  THEN ;

\ Set a cell at an index in a vector and return whether it was successful.
: SET-VECTOR-CELL ( value index vector -- success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  ROT HERE 1 CELLS ALLOT TUCK ! ROT ROT SET-VECTOR -1 CELLS ALLOT ;

\ Set a double cell at an index in a vector and return whether it was
\ successful.
: SET-VECTOR-2CELL ( value1 value2 index vector -- success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  >R >R HERE ROT ROT 2 PICK 2! 2 CELLS ALLOT
  R> R> SET-VECTOR -2 CELLS ALLOT ;

\ Push a block onto the start of a vector and return whether it was successful.
: PUSH-START-VECTOR ( addr vector -- success )
  DUP FULL-VECTOR? NOT IF
    PUSH-START-VECTOR TRUE
  ELSE
    DUP VECTOR-FLAGS @ ALLOCATED-VECTOR AND IF
      DUP EXPAND-VECTOR IF
	PUSH-START-VECTOR TRUE
      ELSE
	2DROP FALSE
      THEN
    ELSE
      2DROP FALSE
    THEN
  THEN ;

\ Push a cell onto the start of a vector and return whether it was successful.
: PUSH-START-VECTOR-CELL ( value vector -- success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT ROT OVER ! SWAP PUSH-START-VECTOR -1 CELLS ALLOT ;

\ Push a double cell onto the start of a vector and return whether it was
\ successful.
: PUSH-START-VECTOR-2CELL ( value1 value2 vector -- success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  ROT ROT HERE DUP >R 2 CELLS ALLOT 2! R> SWAP PUSH-START-VECTOR
  -2 CELLS ALLOT ;

\ Pop a block from the start of a vector and return whether it was successful.
: POP-START-VECTOR ( addr vector -- success )
  DUP EMPTY-VECTOR? NOT IF POP-START-VECTOR TRUE ELSE 2DROP FALSE THEN ;

\ Pop a cell from the start of a vector and return whether it was successful.
: POP-START-VECTOR-CELL ( vector -- value success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT TUCK SWAP POP-START-VECTOR IF @ TRUE ELSE DROP 0 FALSE THEN
  -1 CELLS ALLOT ;

\ Pop a double cell from the start of a vector and return whether it was
\ successful.
: POP-START-VECTOR-2CELL ( vector -- value1 value2 success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  HERE 2 CELLS ALLOT TUCK SWAP POP-START-VECTOR IF
    2@ TRUE
  ELSE
    DROP 0 0 FALSE
  THEN
  -2 CELLS ALLOT ;

\ Drop a block from the start of a vector and return whether it was successful.
: DROP-START-VECTOR ( vector -- success )
  DUP EMPTY-VECTOR? NOT IF
    0 OVER DO-FINALIZE DROP-START-VECTOR TRUE
  ELSE
    DROP FALSE
  THEN ;

\ Peek a block from the start of a vector and return whether it was successful.
: PEEK-START-VECTOR ( addr vector -- success )
  DUP EMPTY-VECTOR? NOT IF PEEK-START-VECTOR TRUE ELSE 2DROP FALSE THEN ;

\ Peek a cell from the start of a vector and return whether it was successful.
: PEEK-START-VECTOR-CELL ( vector -- value success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT TUCK SWAP PEEK-START-VECTOR
  IF @ TRUE ELSE DROP 0 FALSE THEN
  -1 CELLS ALLOT ;

\ Peek a double cell from the start of a vector and return whether it was
\ successful.
: PEEK-START-VECTOR-2CELL ( vector -- value1 value2 success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  HERE 2 CELLS ALLOT TUCK SWAP PEEK-START-VECTOR IF
    2@ TRUE
  ELSE
    DROP 0 0 FALSE
  THEN
  -2 CELLS ALLOT ;

\ Push a block onto the end of a vector and return whether it was successful.
: PUSH-END-VECTOR ( addr vector -- success )
  DUP FULL-VECTOR? NOT IF
    PUSH-END-VECTOR TRUE
  ELSE
    DUP VECTOR-FLAGS @ ALLOCATED-VECTOR AND IF
      DUP EXPAND-VECTOR IF
	PUSH-END-VECTOR TRUE
      ELSE
	2DROP FALSE
      THEN
    ELSE
      2DROP FALSE
    THEN
  THEN ;

\ Push a cell onto the end of a vector and return whether it was successful.
: PUSH-END-VECTOR-CELL ( value vector -- success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT ROT OVER ! SWAP PUSH-END-VECTOR -1 CELLS ALLOT ;

\ Push a double cell onto the end of a vector and return whether it was
\ successful.
: PUSH-END-VECTOR-2CELL ( value1 value2 vector -- success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  ROT ROT HERE DUP >R 2 CELLS ALLOT 2! R> SWAP PUSH-END-VECTOR
  -2 CELLS ALLOT ;

\ Pop a block from the end of a vector and return whether it was successful.
: POP-END-VECTOR ( addr vector -- success )
  DUP EMPTY-VECTOR? NOT IF POP-END-VECTOR TRUE ELSE 2DROP FALSE THEN ;

\ Pop a cell from the end of a vector and return whether it was successful.
: POP-END-VECTOR-CELL ( vector -- value success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT TUCK SWAP POP-END-VECTOR IF @ TRUE ELSE DROP 0 FALSE THEN
  -1 CELLS ALLOT ;

\ Pop a double cell from the end of a vector and return whether it was
\ successful.
: POP-END-VECTOR-2CELL ( vector -- value1 value2 success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  HERE 2 CELLS ALLOT TUCK SWAP POP-END-VECTOR IF
    2@ TRUE
  ELSE
    DROP 0 0 FALSE
  THEN
  -2 CELLS ALLOT ;

\ Drop a block from the end of a vector and return whether it was successful.
: DROP-END-VECTOR ( vector -- success )
  DUP EMPTY-VECTOR? NOT IF
    DUP COUNT-VECTOR 1 - OVER DO-FINALIZE DROP-END-VECTOR TRUE
  ELSE
    DROP FALSE
  THEN ;

\ Peek a block from the end of a vector and return whether it was successful.
: PEEK-END-VECTOR ( addr vector -- success )
  DUP EMPTY-VECTOR? NOT IF PEEK-END-VECTOR TRUE ELSE 2DROP FALSE THEN ;

\ Peek a cell from the end of a vector and return whether it was successful.
: PEEK-END-VECTOR-CELL ( vector -- value success )
  DUP VECTOR-ENTRY-SIZE @ 1 CELLS = AVERTS X-NON-CELL-VECTOR
  HERE 1 CELLS ALLOT TUCK SWAP PEEK-END-VECTOR IF @ TRUE ELSE DROP 0 FALSE THEN
  -1 CELLS ALLOT ;

\ Peek a double cell from the end of a vector and return whether it was
\ successful.
: PEEK-END-VECTOR-2CELL ( vector -- value1 value2 success )
  DUP VECTOR-ENTRY-SIZE @ 2 CELLS = AVERTS X-NON-2CELL-VECTOR
  HERE 2 CELLS ALLOT TUCK SWAP PEEK-END-VECTOR IF
    2@ TRUE
  ELSE
    DROP 0 0 FALSE
  THEN
  -2 CELLS ALLOT ;

\ Mismatched entry sizes exception
: X-ENTRY-SIZE-MISMATCH ( -- ) SPACE ." vector entry size mismatch" CR ;

\ Prepend a vector to another vector
: PREPEND-VECTOR ( source-vector dest-vector -- success )
  OVER VECTOR-ENTRY-SIZE @ OVER VECTOR-ENTRY-SIZE @ =
  AVERTS X-ENTRY-SIZE-MISMATCH
  OVER VECTOR-COUNT @ OVER VECTOR-COUNT @ + OVER EXPAND-MULTIPLE IF
    PREPEND-VECTOR TRUE
  ELSE
    2DROP FALSE
  THEN ;

\ Append a vector to another vector
: APPEND-VECTOR ( source-vector dest-vector -- success )
  OVER VECTOR-ENTRY-SIZE @ OVER VECTOR-ENTRY-SIZE @ =
  AVERTS X-ENTRY-SIZE-MISMATCH
  OVER VECTOR-COUNT @ OVER VECTOR-COUNT @ + OVER EXPAND-MULTIPLE IF
    APPEND-VECTOR TRUE
  ELSE
    2DROP FALSE
  THEN ;

\ Get vector entry size.
: GET-VECTOR-ENTRY-SIZE ( vector -- u ) VECTOR-ENTRY-SIZE @ ;

\ Get vector maximum count.
: GET-VECTOR-MAX-COUNT ( vector -- u ) VECTOR-MAX-COUNT @ ;

BASE ! SET-CURRENT SET-ORDER