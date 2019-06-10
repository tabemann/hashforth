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

WORDLIST CONSTANT MAP-WORDLIST
FORTH-WORDLIST LAMBDA-WORDLIST MAP-WORDLIST 3 SET-ORDER
MAP-WORDLIST SET-CURRENT

WORDLIST CONSTANT MAP-PRIVATE-WORDLIST
FORTH-WORDLIST LAMBDA-WORDLIST MAP-PRIVATE-WORDLIST MAP-WORDLIST
LAMBDA-WORDLIST 5 SET-ORDER
MAP-PRIVATE-WORDLIST SET-CURRENT

\ The map entry structure
BEGIN-STRUCTURE MAP-HEADER-SIZE
  \ The map entry key size
  FIELD: MAP-ENTRY-KEY-SIZE

  \ The map entry value size
  FIELD: MAP-ENTRY-VALUE-SIZE
END-STRUCTURE

\ The map structure
BEGIN-STRUCTURE MAP-SIZE
  \ The number of map entry fields, regardless of whether they are used
  FIELD: MAP-COUNT

  \ The number of actual map entries
  FIELD: MAP-ENTRY-COUNT

  \ The map entries
  FIELD: MAP-ENTRIES

  \ The finalizer word
  FIELD: MAP-FINALIZER

  \ The extra finalizer argument
  FIELD: MAP-FINALIZER-ARG
END-STRUCTURE

\ Get the map entry size
: MAP-ENTRY-SIZE ( entry -- )
  DUP MAP-ENTRY-KEY-SIZE @ SWAP MAP-ENTRY-VALUE-SIZE @ +
  MAP-HEADER-SIZE + ;

\ Calculate a hash for a key
: HASH-KEY ( addr bytes -- hash )
  0 BEGIN OVER 0 > WHILE
    DUP 7 LSHIFT SWAP [ 1 CELLS 8 * 7 - ] LITERAL RSHIFT OR
    2 PICK C@ XOR ROT 1 + ROT 1 - ROT
  REPEAT
  NIP NIP ;

\ Convert a key into an entry key
: GET-ENTRY-KEY ( addr bytes map -- entry-key )
  ROT ROT HASH-KEY SWAP MAP-COUNT @ UMOD ;

\ Get entry address
: GET-ENTRY ( entry-key map -- entry ) MAP-ENTRIES @ SWAP CELLS + ;

\ Compare keys
: COMPARE-KEYS ( addr bytes entry -- match )
  DUP MAP-HEADER-SIZE + SWAP MAP-ENTRY-KEY-SIZE @ EQUAL-STRINGS? ;

\ Get the index for a key; return -1 if key is not found
: GET-INDEX ( addr bytes map -- index )
  2 PICK 2 PICK 2 PICK GET-ENTRY-KEY DUP >R BEGIN
    DUP 2 PICK GET-ENTRY
    DUP @ 0 = IF ( addr bytes map entry-key entry )
      DROP NIP NIP NIP R> DROP TRUE
    ELSE
      4 PICK 4 PICK ROT @ COMPARE-KEYS IF
	NIP NIP NIP R> DROP TRUE
      ELSE
	1 + OVER MAP-COUNT @ UMOD DUP R@ <> IF
	  FALSE
	ELSE
	  2DROP 2DROP R> DROP -1 TRUE
	THEN
      THEN
    THEN
  UNTIL ;

\ Get whether an index is a found index
: IS-FOUND-INDEX ( index map -- ) GET-ENTRY @ 0 <> ;

\ Get the key of an entry
: ENTRY-KEY ( entry -- addr bytes )
  DUP MAP-HEADER-SIZE + SWAP MAP-ENTRY-KEY-SIZE @ ;

\ Get the value of an entry
: ENTRY-VALUE ( entry -- addr bytes )
  DUP MAP-HEADER-SIZE + OVER MAP-ENTRY-KEY-SIZE @ +
  SWAP MAP-ENTRY-VALUE-SIZE @ ;

\ Carry out finalizing of an entry if there is a finalizer xt
: DO-FINALIZE ( entry map -- )
  DUP >R MAP-FINALIZER @ IF
    DUP ENTRY-VALUE ROT ENTRY-KEY R@ MAP-FINALIZER-ARG @
    R> MAP-FINALIZER @ EXECUTE
  ELSE
    DROP R> DROP
  THEN ;

\ Actually set a value in a map
: ACTUALLY-SET-MAP ( addr map -- success )
  OVER ENTRY-KEY 2 PICK GET-INDEX DUP -1 <> IF
    DUP 2 PICK IS-FOUND-INDEX IF
      2DUP SWAP GET-ENTRY @ 2 PICK DO-FINALIZE
      SWAP GET-ENTRY DUP @ FREE! ! TRUE
    ELSE
      1 2 PICK MAP-ENTRY-COUNT +!
      SWAP GET-ENTRY ! TRUE
    THEN
  ELSE
    DROP 2DROP FALSE
  THEN ;

\ Allocate an entry block
: ALLOCATE-ENTRY ( value-addr value-bytes key-addr key-bytes -- block-addr )
  2 PICK OVER + MAP-HEADER-SIZE + ALLOCATE IF
    2DUP MAP-ENTRY-KEY-SIZE !
    3 PICK OVER MAP-ENTRY-VALUE-SIZE !
    ROT OVER MAP-HEADER-SIZE + 3 ROLL MOVE
    ROT OVER MAP-HEADER-SIZE + 2 PICK MAP-ENTRY-KEY-SIZE @ + 3 ROLL MOVE
  ELSE
    2DROP 2DROP 0
  THEN ;

\ Map internal exception
: X-MAP-INTERNAL ( -- ) SPACE ." map internal exception" CR ;

\ Map allocation failed exception
: X-MAP-ALLOCATE-FAILED ( -- ) SPACE ." this should not be seen" CR ;

\ Expand a map
: EXPAND-MAP ( map -- )
  DUP MAP-COUNT @ 2 * CELLS ALLOCATE AVERTS X-MAP-ALLOCATE-FAILED
  HERE MAP-SIZE ALLOT
  2 PICK MAP-COUNT @ 2 * OVER MAP-COUNT !
  0 OVER MAP-ENTRY-COUNT !
  0 OVER MAP-FINALIZER !
  0 OVER MAP-FINALIZER-ARG !
  2DUP MAP-ENTRIES !
  DUP MAP-ENTRIES @ OVER MAP-COUNT @ CELLS 0 FILL
  2 PICK MAP-COUNT @ 0 ?DO
    I 3 PICK GET-ENTRY @ ?DUP IF
      OVER ACTUALLY-SET-MAP AVERTS X-MAP-INTERNAL
    THEN
  LOOP
  DROP MAP-SIZE NEGATE ALLOT
  OVER MAP-ENTRIES @ FREE!
  OVER MAP-COUNT @ 2 * 2 PICK MAP-COUNT !
  SWAP MAP-ENTRIES ! ;

MAP-WORDLIST SET-CURRENT

\ Zero map size specified
: X-ZERO-MAP-SIZE ( -- ) SPACE ." zero not valid map size" CR ;

\ Allocate a map
: ALLOCATE-MAP ( initial-entry-count -- map )
  DUP 0 = IF 2DROP ['] X-ZERO-MAP-SIZE ?RAISE THEN
  MAP-SIZE ALLOCATE!
  2DUP MAP-COUNT !
  0 OVER MAP-ENTRY-COUNT !
  0 OVER MAP-FINALIZER-ARG !
  0 OVER MAP-FINALIZER !
  OVER CELLS ALLOCATE!
  OVER MAP-ENTRIES !
  DUP MAP-ENTRIES @ ROT CELLS 0 FILL ;

\ Set a finalizer
: SET-MAP-FINALIZER ( finalizer finalizer-arg map -- )
  TUCK MAP-FINALIZER-ARG ! MAP-FINALIZER ! ;

\ Clear a map
: CLEAR-MAP ( map -- )
  DUP MAP-COUNT @ 0 ?DO
    I OVER IS-FOUND-INDEX IF
      I OVER GET-ENTRY DUP @ DUP 3 PICK DO-FINALIZE FREE! 0 SWAP !
    THEN
  LOOP
  0 SWAP MAP-ENTRY-COUNT ! ;

\ Destroy a map
: DESTROY-MAP ( map -- )
  DUP CLEAR-MAP DUP MAP-ENTRIES @ FREE! FREE! ;

\ Evaluate an xt for each member of a map; note that the internal state
\ is hidden from the xt, so the xt can transparently access the outside stack
: ITER-MAP ( xt map -- )
  0 BEGIN
    2DUP SWAP MAP-COUNT @ < IF
      2DUP SWAP IS-FOUND-INDEX IF
	2DUP SWAP GET-ENTRY @ SWAP >R SWAP >R SWAP >R
	DUP ENTRY-VALUE ROT ENTRY-KEY R@ EXECUTE
	R> R> R>
      THEN
      1 + FALSE
    ELSE
      DROP 2DROP TRUE
    THEN
  UNTIL ;

\ Get a value from a map
: GET-MAP ( key-addr key-bytes map -- value-addr value-bytes found )
  DUP >R GET-INDEX DUP -1 <> IF
    DUP R@ IS-FOUND-INDEX IF
      R> GET-ENTRY @ ENTRY-VALUE TRUE
    ELSE
      DROP R> DROP 0 0 FALSE
    THEN
  ELSE
    DROP R> DROP 0 0 FALSE
  THEN ;

\ Set a value in a map
: SET-MAP ( value-addr value-bytes key-addr key-bytes map -- success )
  DUP MAP-ENTRY-COUNT @ OVER MAP-COUNT @ 2 / > IF
    DUP ['] EXPAND-MAP TRY DUP ['] X-MAP-ALLOCATE-FAILED = IF
      DROP 0
    THEN
    ?RAISE
  THEN
  >R ALLOCATE-ENTRY ?DUP IF
    DUP R> ACTUALLY-SET-MAP IF
      DROP TRUE
    ELSE
      FREE! FALSE
    THEN
  ELSE
    R> DROP FALSE
  THEN ;

\ Delete a value in a map
: DELETE-MAP ( key-addr key-bytes map -- found )
  DUP >R GET-INDEX DUP -1 <> IF
    DUP R@ IS-FOUND-INDEX IF
      -1 R@ MAP-ENTRY-COUNT +!
      R@ GET-ENTRY DUP @ R> DO-FINALIZE 0 SWAP ! TRUE
    ELSE
      DROP R> DROP FALSE
    THEN
  ELSE
    DROP R> DROP FALSE
  THEN ;

\ Get whether a key is a member of a map
: MEMBER-MAP ( key-addr key-bytes intmap -- found )
  DUP >R GET-INDEX DUP -1 <> IF
    R> IS-FOUND-INDEX
  ELSE
    DROP R> DROP FALSE
  THEN ;

\ Unable to copy map
: X-UNABLE-TO-COPY-MAP ( -- ) SPACE ." unable to copy map" CR ;

\ Copy a map into another map
: COPY-MAP ( source-map dest-map -- )
  [: 4 PICK SET-MAP AVERTS X-UNABLE-TO-COPY-MAP ;]
  ROT ITER-MAP DROP ;

BASE ! SET-CURRENT SET-ORDER
