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

WORDLIST CONSTANT SIXEL-WORDLIST
FORTH-WORDLIST SIXEL-WORDLIST 2 SET-ORDER
SIXEL-WORDLIST SET-CURRENT

1024 CONSTANT SIXEL-OUTPUT-SIZE

$1B CONSTANT ESCAPE
\ BL CONSTANT ESCAPE

BEGIN-STRUCTURE SIXEL-FB-SIZE
  FIELD: SIXEL-FB-WIDTH
  FIELD: SIXEL-FB-HEIGHT
  FIELD: SIXEL-FB-COLORS
  FIELD: SIXEL-FB-COLOR-COUNT
  FIELD: SIXEL-FB-ROW-LENGTH
  FIELD: SIXEL-FB-COLOR-LENGTH
  FIELD: SIXEL-FB-DATA
  FIELD: SIXEL-FB-DATA-CURRENT
  FIELD: SIXEL-FB-DATA-END
  FIELD: SIXEL-FB-COMPRESS
  FIELD: SIXEL-FB-COMPRESS-CURRENT
  FIELD: SIXEL-FB-OUTPUT
  FIELD: SIXEL-FB-OUTPUT-INDEX
END-STRUCTURE

1024 CONSTANT MAX-NUM-COUNT
1024 CONSTANT MAX-RLE-MULTIPLIER-COUNT

CREATE PREFIXED-NUM-SIZES MAX-NUM-COUNT ALLOT
CREATE PREFIXED-NUM-SUMS MAX-NUM-COUNT 2 * ALLOT
CREATE RLE-MULTIPLIERS MAX-RLE-MULTIPLIER-COUNT 5 * ALLOT

: INIT-PREFIXED-NUM-SUMS ( -- )
  0 0 BEGIN DUP MAX-NUM-COUNT < WHILE
    DUP DUP 10 < IF
      DROP 2
    ELSE DUP 100 < IF
      DROP 3
    ELSE 1000 < IF
      4
    ELSE
      5
    THEN THEN THEN
    2DUP SWAP PREFIXED-NUM-SIZES + C! ROT + 2DUP SWAP 2 *
    PREFIXED-NUM-SUMS + H! SWAP 1 +
  REPEAT
  2DROP ;

INIT-PREFIXED-NUM-SUMS

: INIT-RLE-MULTIPLIERS ( -- )
  0 BEGIN DUP MAX-RLE-MULTIPLIER-COUNT < WHILE
    [CHAR] ! OVER 5 * RLE-MULTIPLIERS + C!
    DUP FORMAT-NUMBER 2 PICK 5 * 1 + RLE-MULTIPLIERS + SWAP MOVE
    1 +
  REPEAT
  DROP ;

INIT-RLE-MULTIPLIERS

: BUFFER-CHAR ( c fb -- )
  DUP SIXEL-FB-OUTPUT-INDEX @ SIXEL-OUTPUT-SIZE >= IF
    DUP SIXEL-FB-OUTPUT @ SIXEL-OUTPUT-SIZE TYPE
    1 OVER SIXEL-FB-OUTPUT-INDEX ! SIXEL-FB-OUTPUT @ C!
  ELSE
    DUP SIXEL-FB-OUTPUT-INDEX @ >R TUCK SIXEL-FB-OUTPUT @ R@ + C!
    R> 1 + SWAP SIXEL-FB-OUTPUT-INDEX !
  THEN ;

: BUFFER-STRING ( c-addr u fb -- )
  >R BEGIN DUP 0 > WHILE
    1 - SWAP DUP C@ R@ BUFFER-CHAR 1 + SWAP
  REPEAT
  R> DROP 2DROP ;

: BUFFER-DECIMAL ( n fb -- )
  SWAP ['] FORMAT-NUMBER 10 BASE-EXECUTE ROT BUFFER-STRING ;

: FLUSH-BUFFER ( fb -- )
  DUP SIXEL-FB-OUTPUT @ OVER SIXEL-FB-OUTPUT-INDEX @ TYPE
  0 SWAP SIXEL-FB-OUTPUT-INDEX ! ;

: BEGIN-FRAME ( fb -- )
  ESCAPE OVER BUFFER-CHAR [CHAR] P OVER BUFFER-CHAR [CHAR] q SWAP BUFFER-CHAR ;

: END-FRAME ( -- ) ESCAPE EMIT [CHAR] \ EMIT ;

: CLEAR-COLORS ( fb -- )
  >R 0 BEGIN DUP R@ SIXEL-FB-COLOR-COUNT @ < WHILE
    DUP CELLS R@ SIXEL-FB-COLORS @ + 0 SWAP ! 1 +
  REPEAT
  DROP R> DROP ;

: SET-COLOR ( r g b color fb -- )
  SIXEL-FB-COLORS @ SWAP CELLS +
  SWAP $FF AND ROT $FF AND 8 LSHIFT OR ROT $FF AND 16 LSHIFT OR SWAP ! ;

: GET-COLOR ( color fb -- r g b )
  SIXEL-FB-COLORS @ SWAP CELLS + @ >R
  R@ 16 RSHIFT R@ 8 RSHIFT $FF AND R> $FF AND ;

: GENERATE-PALETTE-ENTRY ( r g b color fb --)
  [CHAR] # OVER BUFFER-CHAR TUCK BUFFER-DECIMAL
  [CHAR] ; OVER BUFFER-CHAR [CHAR] 2 OVER BUFFER-CHAR
  [CHAR] ; OVER BUFFER-CHAR 3 ROLL 100 * 255 / OVER BUFFER-DECIMAL
  [CHAR] ; OVER BUFFER-CHAR ROT 100 * 255 / OVER BUFFER-DECIMAL
  [CHAR] ; OVER BUFFER-CHAR SWAP 100 * 255 / SWAP BUFFER-DECIMAL ;

: GENERATE-PALETTE ( fb -- )
  >R 0 BEGIN DUP R@ SIXEL-FB-COLOR-COUNT @ < WHILE
    DUP R@ GET-COLOR 3 PICK R@ GENERATE-PALETTE-ENTRY 1 +
  REPEAT
  DROP R> DROP ;

: GET-DATA-SIZE ( fb -- bytes )
  >R R@ SIXEL-FB-COLOR-COUNT @ R@ SIXEL-FB-WIDTH @ 1 + *
  R@ SIXEL-FB-HEIGHT @ 6 / *
  R@ SIXEL-FB-HEIGHT @ 6 /
  R@ SIXEL-FB-COLOR-COUNT @ 1 - 2 * PREFIXED-NUM-SUMS + H@ * +
  R> SIXEL-FB-HEIGHT @ 6 / + ;

: GET-ROW-LENGTH ( fb -- bytes )
  >R R@ SIXEL-FB-COLOR-COUNT @ R@ SIXEL-FB-WIDTH @ 1 + * 1 +
  R> SIXEL-FB-COLOR-COUNT @ 1 - 2 * PREFIXED-NUM-SUMS + H@ + ;

: GET-COLOR-LENGTH ( fb -- bytes ) SIXEL-FB-WIDTH @ 1 + ;

: GET-ADDRESS ( x y color fb -- addr )
  >R R@ SIXEL-FB-ROW-LENGTH @ ROT 6 / *
  OVER 2 * PREFIXED-NUM-SUMS + H@ +
  SWAP R@ SIXEL-FB-COLOR-LENGTH @ * + + R> SIXEL-FB-DATA @ + ;

: CLEAR-PIXEL ( x y fb -- )
  >R R@ SIXEL-FB-ROW-LENGTH @ OVER 6 / * R@ SIXEL-FB-DATA @ + ROT +
  SWAP 6 MOD SWAP
  0 BEGIN DUP R@ SIXEL-FB-COLOR-COUNT @ < WHILE
    SWAP OVER PREFIXED-NUM-SIZES + C@ + SWAP
    OVER C@ 63 - 1 3 PICK LSHIFT NOT AND 63 + 2 PICK C!
    1 + SWAP R@ SIXEL-FB-COLOR-LENGTH @ + SWAP
  REPEAT
  2DROP DROP R> DROP ;

: PIXEL! ( x y color fb -- )
  3 PICK 3 PICK 2 PICK CLEAR-PIXEL
  2 PICK >R GET-ADDRESS DUP C@ 63 - 1 R> 6 MOD LSHIFT OR 63 + SWAP C! ;

: PIXEL-NO-CLEAR! ( x y color fb -- )
  2 PICK >R GET-ADDRESS DUP C@ 63 - 1 R> 6 MOD LSHIFT OR 63 + SWAP C! ;

: POPULATE-LF ( y/6 fb -- )
  >R R@ SIXEL-FB-ROW-LENGTH @ SWAP 1 + * 1 - R> SIXEL-FB-DATA @ +
  [CHAR] - SWAP C! ;

: POPULATE-CR ( y/6 color fb -- )
  >R DUP 2 * PREFIXED-NUM-SUMS + H@
  R@ SIXEL-FB-ROW-LENGTH @ 3 PICK * + OVER R@ SIXEL-FB-COLOR-LENGTH @ * +
  R@ SIXEL-FB-WIDTH @ + R> SIXEL-FB-DATA @ + [CHAR] $ SWAP C! 2DROP ;

: POPULATE-COLOR ( y/6 color fb -- )
  >R DUP 2 * PREFIXED-NUM-SUMS + H@ OVER PREFIXED-NUM-SIZES + C@ -
  R@ SIXEL-FB-ROW-LENGTH @ 3 PICK * + OVER R@ SIXEL-FB-COLOR-LENGTH @ * +
  R> SIXEL-FB-DATA @ + [CHAR] # OVER C! 1 +
  OVER ['] FORMAT-NUMBER 10 BASE-EXECUTE ROT SWAP MOVE 2DROP ;

: CLEAR-LINE-COLOR ( y/6 color fb -- )
  >R DUP 2 * PREFIXED-NUM-SUMS + H@
  R@ SIXEL-FB-ROW-LENGTH @ 3 PICK * + OVER R@ SIXEL-FB-COLOR-LENGTH @ * +
  R@ SIXEL-FB-DATA @ +
  R> SIXEL-FB-WIDTH @ 63 FILL 2DROP ;

: FILL-LINE-COLOR ( y/6 color fb -- )
  >R DUP 2 * PREFIXED-NUM-SUMS + H@
  R@ SIXEL-FB-ROW-LENGTH @ 3 PICK * + OVER R@ SIXEL-FB-COLOR-LENGTH @ * +
  R@ SIXEL-FB-DATA @ +
  R> SIXEL-FB-WIDTH @ 126 FILL 2DROP ;

: CLEAR-PIXELS ( fb -- )
  >R 0 BEGIN DUP R@ SIXEL-FB-HEIGHT @ 6 / < WHILE
    0 BEGIN DUP R@ SIXEL-FB-COLOR-COUNT @ < WHILE
      2DUP R@ CLEAR-LINE-COLOR 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP R> DROP ;

: FILL-PIXELS ( color fb -- )
  >R 0 BEGIN DUP R@ SIXEL-FB-HEIGHT @ 6 / < WHILE
    0 BEGIN DUP R@ SIXEL-FB-COLOR-COUNT @ < WHILE
      DUP 3 PICK = IF
        2DUP R@ FILL-LINE-COLOR
      ELSE
        2DUP R@ CLEAR-LINE-COLOR
      THEN
      1 +
    REPEAT
    DROP 1 +
  REPEAT
  2DROP R> DROP ;

: INIT-PIXELS ( fb -- )
  >R 0 BEGIN DUP R@ SIXEL-FB-HEIGHT @ 6 / < WHILE
    DUP R@ POPULATE-LF
    0 BEGIN DUP R@ SIXEL-FB-COLOR-COUNT @ < WHILE
      2DUP R@ POPULATE-CR
      2DUP R@ POPULATE-COLOR
      2DUP R@ CLEAR-LINE-COLOR
      1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP R> DROP ;

: GET-RUN-LENGTH ( fb -- )
  SIXEL-FB-DATA-CURRENT @ DUP C@ >R 1 + 1 BEGIN
    DUP MAX-NUM-COUNT < IF
      OVER C@ R@ = IF
        SWAP 1 + SWAP 1 + FALSE
      ELSE
        TRUE
      THEN
    ELSE
      TRUE
    THEN
  UNTIL
  NIP R> DROP ;

: COMPRESS ( fb -- )
  >R BEGIN R@ SIXEL-FB-DATA-CURRENT @ R@ SIXEL-FB-DATA-END @ < WHILE
    R@ SIXEL-FB-DATA-CURRENT @ C@ DUP 63 < OVER 126 > OR IF
      R@ SIXEL-FB-COMPRESS-CURRENT @ C!
      1 R@ SIXEL-FB-DATA-CURRENT +!
      1 R@ SIXEL-FB-COMPRESS-CURRENT +!
    ELSE
      R@ GET-RUN-LENGTH DUP PREFIXED-NUM-SIZES + C@ 1 + OVER < IF
        DUP 5 * RLE-MULTIPLIERS + R@ SIXEL-FB-COMPRESS-CURRENT @
	2 PICK PREFIXED-NUM-SIZES + C@ DUP >R CMOVE
	R> R@ SIXEL-FB-COMPRESS-CURRENT +!
	R@ SIXEL-FB-DATA-CURRENT +!
	R@ SIXEL-FB-COMPRESS-CURRENT @ C!
	1 R@ SIXEL-FB-COMPRESS-CURRENT +!
      ELSE
        R@ SIXEL-FB-COMPRESS-CURRENT @ OVER 3 ROLL FILL
	DUP R@ SIXEL-FB-DATA-CURRENT +!
	R@ SIXEL-FB-COMPRESS-CURRENT +!
      THEN
    THEN
  REPEAT
  R> DROP ;

: DRAW ( fb -- )
  DUP BEGIN-FRAME DUP GENERATE-PALETTE DUP FLUSH-BUFFER
  DUP SIXEL-FB-DATA @ OVER SIXEL-FB-DATA-CURRENT !
  DUP SIXEL-FB-COMPRESS @ OVER SIXEL-FB-COMPRESS-CURRENT !
  DUP COMPRESS
  DUP SIXEL-FB-COMPRESS @ SWAP SIXEL-FB-COMPRESS-CURRENT @ OVER - TYPE
  END-FRAME ;

: NEW-SIXEL-FB ( width height colors -- fb )
  >R DUP 6 MOD DUP 0 > IF 6 SWAP - + ELSE DROP THEN
  HERE SIXEL-FB-SIZE ALLOT
  R> OVER SIXEL-FB-COLOR-COUNT ! TUCK SIXEL-FB-HEIGHT ! TUCK SIXEL-FB-WIDTH !
  DUP GET-ROW-LENGTH OVER SIXEL-FB-ROW-LENGTH !
  DUP GET-COLOR-LENGTH OVER SIXEL-FB-COLOR-LENGTH !
  HERE >R DUP SIXEL-FB-COLOR-COUNT @ CELLS ALLOT R> OVER SIXEL-FB-COLORS !
  DUP CLEAR-COLORS
  HERE >R DUP GET-DATA-SIZE ALLOT R> OVER SIXEL-FB-DATA !
  DUP SIXEL-FB-DATA @ OVER SIXEL-FB-DATA-CURRENT !
  DUP SIXEL-FB-DATA @ OVER GET-DATA-SIZE + OVER SIXEL-FB-DATA-END !
  DUP INIT-PIXELS
  HERE >R DUP GET-DATA-SIZE ALLOT R> OVER SIXEL-FB-COMPRESS !
  DUP SIXEL-FB-COMPRESS @ OVER SIXEL-FB-COMPRESS-CURRENT !
  HERE >R SIXEL-OUTPUT-SIZE ALLOT R> OVER SIXEL-FB-OUTPUT !
  0 OVER SIXEL-FB-OUTPUT-INDEX ! ;

BASE ! SET-CURRENT SET-ORDER