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

WORDLIST CONSTANT DATAMAP-WORDLIST
FORTH-WORDLIST LAMBDA-WORDLIST DATAMAP-WORDLIST 3 SET-ORDER
DATAMAP-WORDLIST SET-CURRENT

WORDLIST CONSTANT DATAMAP-PRIVATE-WORDLIST
FORTH-WORDLIST LAMBDA-WORDLIST DATAMAP-WORDLIST DATAMAP-PRIVATE-WORDLIST
LAMBDA-WORDLIST 5 SET-ORDER
DATAMAP-PRIVATE-WORDLIST SET-CURRENT

\ The datamap entry structure
BEGIN-STRUCTURE DATAMAP-HEADER-SIZE
  \ The datamap entry key size
  FIELD: DATAMAP-ENTRY-KEY-SIZE

  \ The datamap entry value size
  FIELD: DATAMAP-ENTRY-VALUE-SIZE
END-STRUCTURE

\ The datamap structure
BEGIN-STRUCTURE DATAMAP-SIZE
  \ The number of datamap entry fields, regardless of whether they are used
  FIELD: DATAMAP-COUNT

  \ The number of actual datamap entries
  FIELD: DATAMAP-ENTRY-COUNT

  \ The datamap entries
  FIELD: DATAMAP-ENTRIES

  \ The finalizer word
  FIELD: DATAMAP-FINALIZER

  \ The extra finalizer argument
  FIELD: DATAMAP-FINALIZER-ARG
END-STRUCTURE

\ Get the datamap entry size
: DATAMAP-ENTRY-SIZE ( entry -- )
  DUP DATAMAP-ENTRY-KEY-SIZE @ SWAP DATAMAP-ENTRY-VALUE-SIZE @ +
  DATAMAP-HEADER-SIZE + ;

\ Calculate a hash for a key
: HASH-KEY ( addr bytes -- hash )
  0 BEGIN OVER 0 > WHILE
    DUP 7 LSHIFT SWAP [ 1 CELLS 8 * 7 - ] LITERAL RSHIFT OR
    2 PICK C@ XOR ROT 1 + ROT 1 - ROT
  REPEAT
  NIP NIP ;

\ Convert a key into an entry key
: GET-ENTRY-KEY ( addr bytes datamap -- entry-key )
  ROT ROT HASH-KEY SWAP DATAMAP-COUNT @ UMOD ;

\ Get entry address
: GET-ENTRY ( entry-key datamap -- entry ) DATAMAP-ENTRIES @ SWAP CELLS + ;

\ Compare keys
: COMPARE-KEYS ( addr bytes entry -- match )
  DUP DATAMAP-HEADER-SIZE + SWAP DATAMAP-ENTRY-KEY-SIZE @ EQUAL-STRINGS? ;

\ Get the index for a key; return -1 if key is not found
: GET-INDEX ( addr bytes datamap -- index )
  2 PICK 2 PICK 2 PICK GET-ENTRY-KEY DUP >R BEGIN
    DUP 2 PICK GET-ENTRY
    DUP @ 0 = IF ( addr bytes datamap entry-key entry )
      DROP NIP NIP NIP R> DROP TRUE
    ELSE
      4 PICK 4 PICK ROT @ COMPARE-KEYS IF
	NIP NIP NIP R> DROP TRUE
      ELSE
	1 + OVER DATAMAP-COUNT @ UMOD DUP R@ <> IF
	  FALSE
	ELSE
	  2DROP 2DROP R> DROP -1 TRUE
	THEN
      THEN
    THEN
  UNTIL ;

\ Get whether an index is a found index
: IS-FOUND-INDEX ( index datamap -- ) GET-ENTRY @ 0 <> ;

\ Get the key of an entry
: ENTRY-KEY ( entry -- addr bytes )
  DUP DATAMAP-HEADER-SIZE + SWAP DATAMAP-ENTRY-KEY-SIZE @ ;

\ Get the value of an entry
: ENTRY-VALUE ( entry -- addr bytes )
  DUP DATAMAP-HEADER-SIZE + OVER DATAMAP-ENTRY-KEY-SIZE @ +
  SWAP DATAMAP-ENTRY-VALUE-SIZE @ ;

\ Carry out finalizing of an entry if there is a finalizer xt
: DO-FINALIZE ( entry datamap -- )
  DUP >R DATAMAP-FINALIZER @ IF
    DUP ENTRY-KEY ROT ENTRY-VALUE R@ DATAMAP-FINALIZER-ARG @
    R> DATAMAP-FINALIZER @ EXECUTE
  ELSE
    DROP R> DROP
  THEN ;

\ Actually set a value in a datamap
: ACTUALLY-SET-DATAMAP ( addr datamap -- success )
  OVER ENTRY-KEY 2 PICK GET-INDEX DUP -1 <> IF
    DUP 2 PICK IS-FOUND-INDEX IF
      2DUP SWAP GET-ENTRY @ 2 PICK DO-FINALIZE
      SWAP GET-ENTRY DUP @ FREE! ! TRUE
    ELSE
      1 2 PICK DATAMAP-ENTRY-COUNT +!
      SWAP GET-ENTRY ! TRUE
    THEN
  ELSE
    DROP 2DROP FALSE
  THEN ;

\ Allocate an entry block
: ALLOCATE-ENTRY ( value-addr value-bytes key-addr key-bytes -- block-addr )
  2 PICK OVER + DATAMAP-HEADER-SIZE + ALLOCATE IF
    2DUP DATAMAP-ENTRY-KEY-SIZE !
    3 PICK OVER DATAMAP-ENTRY-VALUE-SIZE !
    ROT OVER DATAMAP-HEADER-SIZE + 3 ROLL MOVE
    ROT OVER DATAMAP-HEADER-SIZE + 2 PICK DATAMAP-ENTRY-KEY-SIZE @ + 3 ROLL MOVE
  ELSE
    2DROP 2DROP 0
  THEN ;

\ Datamap internal exception
: X-DATAMAP-INTERNAL ( -- ) SPACE ." datamap internal exception" CR ;

\ Datamap allocation failed exception
: X-DATAMAP-ALLOCATE-FAILED ( -- ) SPACE ." this should not be seen" CR ;

\ Expand a datamap
: EXPAND-DATAMAP ( datamap -- )
  DUP DATAMAP-COUNT @ 2 * CELLS ALLOCATE AVERTS X-DATAMAP-ALLOCATE-FAILED
  HERE DATAMAP-SIZE ALLOT
  2 PICK DATAMAP-COUNT @ 2 * OVER DATAMAP-COUNT !
  0 OVER DATAMAP-ENTRY-COUNT !
  0 OVER DATAMAP-FINALIZER !
  2DUP DATAMAP-ENTRIES !
  DUP DATAMAP-ENTRIES @ OVER DATAMAP-COUNT @ CELLS 0 FILL
  2 PICK DATAMAP-COUNT @ 0 ?DO
    I 3 PICK GET-ENTRY @ ?DUP IF
      OVER ACTUALLY-SET-DATAMAP AVERTS X-DATAMAP-INTERNAL
    THEN
  LOOP
  DROP DATAMAP-SIZE NEGATE ALLOT
  OVER DATAMAP-ENTRIES @ FREE!
  OVER DATAMAP-COUNT @ 2 * 2 PICK DATAMAP-COUNT !
  SWAP DATAMAP-ENTRIES ! ;

DATAMAP-WORDLIST SET-CURRENT

\ Zero datamap size specified
: X-ZERO-DATAMAP-SIZE ( -- ) SPACE ." zero not valid datamap size" CR ;

\ Allocate a datamap
: ALLOCATE-DATAMAP ( initial-entry-count -- datamap )
  DUP 0 = IF 2DROP ['] X-ZERO-DATAMAP-SIZE ?RAISE THEN
  DATAMAP-SIZE ALLOCATE!
  2DUP DATAMAP-COUNT !
  0 OVER DATAMAP-ENTRY-COUNT !
  0 OVER DATAMAP-FINALIZER-ARG !
  0 OVER DATAMAP-FINALIZER !
  OVER CELLS ALLOCATE!
  OVER DATAMAP-ENTRIES !
  DUP DATAMAP-ENTRIES @ ROT CELLS 0 FILL ;

\ Set a finalizer
: SET-DATAMAP-FINALIZER ( finalizer finalizer-arg datamap )
  TUCK DATAMAP-FINALIZER-ARG ! DATAMAP-FINALIZER ! ;

\ Clear a datamap
: CLEAR-DATAMAP ( datamap -- )
  DUP DATAMAP-COUNT @ 0 ?DO
    I OVER IS-FOUND-INDEX IF
      I OVER GET-ENTRY DUP @ DUP 3 PICK DO-FINALIZE FREE! 0 SWAP !
    THEN
  LOOP
  0 SWAP DATAMAP-ENTRY-COUNT ! ;

\ Destroy a datamap
: DESTROY-DATAMAP ( datamap -- )
  DUP CLEAR-DATAMAP DUP DATAMAP-ENTRIES @ FREE! FREE! ;

\ Get a value from a datamap
: GET-DATAMAP ( key-addr key-bytes datamap -- value-addr value-bytes found )
  DUP >R GET-INDEX DUP -1 <> IF
    DUP R@ IS-FOUND-INDEX IF
      R> GET-ENTRY @ ENTRY-VALUE TRUE
    ELSE
      DROP R> DROP 0 0 FALSE
    THEN
  ELSE
    DROP R> DROP 0 0 FALSE
  THEN ;

\ Set a value in a datamap
: SET-DATAMAP ( value-addr value-bytes key-addr key-bytes datamap -- success )
  DUP DATAMAP-ENTRY-COUNT @ OVER DATAMAP-COUNT @ 2 / > IF
    DUP ['] EXPAND-DATAMAP TRY DUP ['] X-DATAMAP-ALLOCATE-FAILED = IF
      DROP 0
    THEN
    ?RAISE
  THEN
  >R ALLOCATE-ENTRY ?DUP IF
    DUP R> ACTUALLY-SET-DATAMAP IF
      DROP TRUE
    ELSE
      FREE! FALSE
    THEN
  ELSE
    R> DROP FALSE
  THEN ;

\ Delete a value in a datamap
: DELETE-DATAMAP ( key-addr key-bytes datamap -- found )
  DUP >R GET-INDEX DUP -1 <> IF
    DUP R@ IS-FOUND-INDEX IF
      -1 R@ DATAMAP-ENTRY-COUNT +!
      R@ GET-ENTRY DUP @ R> DO-FINALIZE 0 SWAP ! TRUE
    ELSE
      DROP R> DROP FALSE
    THEN
  ELSE
    DROP R> DROP FALSE
  THEN ;

\ Get whether a key is a member of a datamap
: MEMBER-DATAMAP ( key-addr key-bytes intmap -- found )
  DUP >R GET-INDEX DUP -1 <> IF
    R> IS-FOUND-INDEX
  ELSE
    DROP R> DROP FALSE
  THEN ;

BASE ! SET-CURRENT SET-ORDER
