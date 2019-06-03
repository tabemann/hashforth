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
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT INTMAP-WORDLIST
FORTH-WORDLIST LAMBDA-WORDLIST INTMAP-WORDLIST 3 SET-ORDER
INTMAP-WORDLIST SET-CURRENT

WORDLIST CONSTANT INTMAP-PRIVATE-WORDLIST
FORTH-WORDLIST LAMBDA-WORDLIST INTMAP-WORDLIST INTMAP-PRIVATE-WORDLIST
LAMBDA-WORDLIST 5 SET-ORDER
INTMAP-PRIVATE-WORDLIST SET-CURRENT

\ The intmap entry structure
BEGIN-STRUCTURE INTMAP-HEADER-SIZE
  \ The intmap entry key (+ 1, a key of 0 indicates that an entry is free)
  FIELD: INTMAP-ENTRY-KEY
END-STRUCTURE

\ The intmap structure
BEGIN-STRUCTURE INTMAP-SIZE
  \ The intmap flags
  FIELD: INTMAP-FLAGS

  \ The intmap value size
  FIELD: INTMAP-VALUE-SIZE
  
  \ The number of intmap entry fields, regardless of whether they are used
  FIELD: INTMAP-COUNT

  \ The number of actual intmap entries
  FIELD: INTMAP-ENTRY-COUNT

  \ The intmap entries
  FIELD: INTMAP-ENTRIES
END-STRUCTURE

\ Get the intmap entry size
: INTMAP-ENTRY-SIZE ( intmap -- bytes )
  INTMAP-VALUE-SIZE @ INTMAP-HEADER-SIZE + ;

\ Convert a key into an entry key
: GET-ENTRY-KEY ( key intmap -- entry-key ) INTMAP-COUNT @ UMOD ;

\ Get entry address
: GET-ENTRY ( entry-key intmap -- entry )
  DUP INTMAP-ENTRIES @ ROT ROT INTMAP-ENTRY-SIZE * + ;

\ Get the index for a key; returns -1 if key is not found
: GET-INDEX ( key intmap -- index )
  2DUP GET-ENTRY-KEY DUP >R BEGIN
    DUP 2 PICK GET-ENTRY
    DUP INTMAP-ENTRY-KEY @ 0 = IF ( key intmap entry-key entry )
      DROP NIP NIP R> DROP TRUE ( [: ." a " ;] as-non-task-io )
    ELSE
      DUP INTMAP-ENTRY-KEY @ 4 PICK 1 + = IF
	DROP NIP NIP R> DROP TRUE ( [: ." b " ;] as-non-task-io )
      ELSE
	DROP 1 + OVER INTMAP-COUNT @ UMOD DUP R@ <> IF
	  FALSE ( [: ." c " ;] as-non-task-io )
	ELSE
	  2DROP DROP R> DROP -1 TRUE ( [: ." d " ;] as-non-task-io )
	THEN
      THEN
    THEN
  UNTIL ;

\ Get whether an index is a found index
: IS-FOUND-INDEX ( index intmap -- ) GET-ENTRY INTMAP-ENTRY-KEY @ 0 <> ;

\ The intmap allocated flag
1 CONSTANT INTMAP-ALLOCATED

\ Actually set a value in an intmap
: ACTUALLY-SET-INTMAP ( addr key intmap -- success )
  2DUP GET-INDEX DUP -1 <> IF
    DUP 2 PICK IS-FOUND-INDEX IF
      OVER GET-ENTRY ROT DROP
      INTMAP-HEADER-SIZE + ROT SWAP ROT INTMAP-VALUE-SIZE @ MOVE TRUE
    ELSE
      1 2 PICK INTMAP-ENTRY-COUNT +!
      OVER GET-ENTRY ROT 1 + OVER INTMAP-ENTRY-KEY !
      INTMAP-HEADER-SIZE + ROT SWAP ROT INTMAP-VALUE-SIZE @ MOVE TRUE
    THEN
  ELSE
    2DROP 2DROP FALSE
  THEN ;

\ Intmap internal exception
: X-INTMAP-INTERNAL ( -- ) SPACE ." intmap internal exception" CR ;

\ Intmap allocation failed exception
: X-INTMAP-ALLOCATE-FAILED ( -- ) SPACE ." this should not be seen" CR ;

\ Expand a heap-allocated intmap
: EXPAND-INTMAP ( intmap -- )
  DUP INTMAP-COUNT @ 2 * OVER INTMAP-ENTRY-SIZE *
  ALLOCATE AVERTS X-INTMAP-ALLOCATE-FAILED
  HERE INTMAP-SIZE ALLOT
  0 OVER INTMAP-FLAGS !
  2 PICK INTMAP-VALUE-SIZE @ OVER INTMAP-VALUE-SIZE !
  2 PICK INTMAP-COUNT @ 2 * OVER INTMAP-COUNT !
  0 OVER INTMAP-ENTRY-COUNT !
  2DUP INTMAP-ENTRIES !
  DUP INTMAP-ENTRIES @ OVER INTMAP-COUNT @ 2 PICK INTMAP-ENTRY-SIZE *
  0 FILL
  2 PICK INTMAP-COUNT @ 0 ?DO
    I 3 PICK GET-ENTRY DUP INTMAP-ENTRY-KEY @ 0 <> IF
      DUP INTMAP-HEADER-SIZE + SWAP INTMAP-ENTRY-KEY @ 1 -
      2 PICK ACTUALLY-SET-INTMAP AVERTS X-INTMAP-INTERNAL
    ELSE
      DROP
    THEN
  LOOP
  DROP INTMAP-SIZE NEGATE ALLOT
  OVER INTMAP-ENTRIES @ FREE!
  OVER INTMAP-COUNT @ 2 * 2 PICK INTMAP-COUNT !
  SWAP INTMAP-ENTRIES ! ;

INTMAP-WORDLIST SET-CURRENT

\ Zero intmap size specified
: X-ZERO-INTMAP-SIZE ( -- ) SPACE ." zero not valid intmap size" CR ;

\ Zero value size specified
: X-ZERO-VALUE-SIZE ( -- ) SPACE ." zero not valid value size" CR ;

\ Allot an intmap
: ALLOT-INTMAP ( value-size max-entry-count -- intmap )
  DUP 0 = IF 2DROP ['] X-ZERO-INTMAP-SIZE ?RAISE THEN
  OVER 0 = IF 2DROP ['] X-ZERO-VALUE-SIZE ?RAISE THEN
  ALIGN HERE INTMAP-SIZE ALLOT
  ROT OVER INTMAP-VALUE-SIZE !
  2DUP INTMAP-COUNT !
  0 OVER INTMAP-ENTRY-COUNT !
  0 OVER INTMAP-FLAGS !
  ALIGN HERE 2 PICK 2 PICK INTMAP-ENTRY-SIZE * ALLOT
  OVER INTMAP-ENTRIES !
  DUP INTMAP-ENTRIES @ ROT 2 PICK INTMAP-ENTRY-SIZE * 0 FILL ;

\ Allocate an intmap
: ALLOCATE-INTMAP ( value-size initial-entry-count -- intmap )
  DUP 0 = IF 2DROP ['] X-ZERO-INTMAP-SIZE ?RAISE THEN
  OVER 0 = IF 2DROP ['] X-ZERO-VALUE-SIZE ?RAISE THEN
  INTMAP-SIZE ALLOCATE!
  ROT OVER INTMAP-VALUE-SIZE !
  2DUP INTMAP-COUNT !
  0 OVER INTMAP-ENTRY-COUNT !
  INTMAP-ALLOCATED OVER INTMAP-FLAGS !
  2DUP INTMAP-ENTRY-SIZE * ALLOCATE!
  OVER INTMAP-ENTRIES !
  DUP INTMAP-ENTRIES @ ROT 2 PICK INTMAP-ENTRY-SIZE * 0 FILL ;

\ Clear an intmap
: CLEAR-INTMAP ( intmap -- )
  DUP INTMAP-ENTRIES @ OVER INTMAP-COUNT @ ROT INTMAP-ENTRY-SIZE * 0 FILL ;

\ Intmap is not allocated exception
: X-INTMAP-NOT-ALLOCATED ( -- ) SPACE ." intmap is not allocated" CR ;

\ Destroy an allocated intmap
: DESTROY-INTMAP ( intmap -- )
  DUP INTMAP-FLAGS @ INTMAP-ALLOCATED AND 0 = IF
    DROP ['] X-INTMAP-NOT-ALLOCATED ?RAISE
  THEN
  DUP INTMAP-ENTRIES @ FREE!
  FREE! ;

\ Get a value from an intmap
: GET-INTMAP ( addr key intmap -- found )
  TUCK GET-INDEX DUP -1 <> IF
    DUP 2 PICK IS-FOUND-INDEX IF
      OVER GET-ENTRY INTMAP-HEADER-SIZE +
      ROT ROT INTMAP-VALUE-SIZE @ MOVE TRUE
    ELSE
      DROP 2DROP FALSE
    THEN
  ELSE
    DROP 2DROP FALSE
  THEN ;

\ Set a value in an intmap
: SET-INTMAP ( addr key intmap -- success )
  DUP INTMAP-FLAGS @ INTMAP-ALLOCATED AND IF
    DUP INTMAP-ENTRY-COUNT @ OVER INTMAP-COUNT @ 2 / > IF
      DUP ['] EXPAND-INTMAP TRY DUP ['] X-INTMAP-ALLOCATE-FAILED = IF
	DROP 0
      THEN
      ?RAISE
    THEN
  THEN
  ACTUALLY-SET-INTMAP ;

\ Delete a value in an intmap
: DELETE-INTMAP ( key intmap -- found )
  TUCK GET-INDEX DUP -1 <> IF
    DUP 2 PICK IS-FOUND-INDEX IF
      -1 2 PICK INTMAP-ENTRY-COUNT +!
      SWAP GET-ENTRY 0 SWAP INTMAP-ENTRY-KEY ! TRUE
    ELSE
      2DROP FALSE
    THEN
  ELSE
    2DROP FALSE
  THEN ;

\ Get whether a key is a member of an intmap
: MEMBER-INTMAP ( key intmap -- found )
  TUCK GET-INDEX DUP -1 <> IF
    SWAP IS-FOUND-INDEX
  ELSE
    2DROP FALSE
  THEN ;

\ Get a cell from an intmap
: GET-INTMAP-CELL ( key intmap -- found value )
  HERE DUP >R 1 CELLS ALLOT ROT ROT GET-INTMAP -1 CELLS ALLOT R> @ SWAP IF
    TRUE
  ELSE
    DROP 0 FALSE
  THEN ;

\ Set a cell in an intmap
: SET-INTMAP-CELL ( value key intmap -- success )
  ROT HERE TUCK ! 1 CELLS ALLOT ROT ROT SET-INTMAP -1 CELLS ALLOT ;

BASE ! SET-CURRENT SET-ORDER