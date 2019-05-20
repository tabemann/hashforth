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

INCLUDE src/hashforth/sixel.fs

WORDLIST CONSTANT LIFE-WORDLIST
FORTH-WORDLIST SIXEL-WORDLIST LIFE-WORDLIST 3 SET-ORDER
LIFE-WORDLIST SET-CURRENT

\ Some basic constants
512 CONSTANT WORLD-WIDTH
512 CONSTANT WORLD-HEIGHT
WORLD-WIDTH WORLD-HEIGHT * CONSTANT WORLD-SIZE
2 CONSTANT COLOR-COUNT

\ Our canvas
WORLD-WIDTH 2 - WORLD-HEIGHT 2 - COLOR-COUNT NEW-SIXEL-FB CONSTANT CANVAS

\ Our (double-buffered) forth world
CREATE WORLD-0 WORLD-SIZE ALLOT
CREATE WORLD-1 WORLD-SIZE ALLOT
VARIABLE CURRENT-WORLD WORLD-0 CURRENT-WORLD !
VARIABLE NEXT-WORLD WORLD-1 NEXT-WORLD !

\ The escape character
$1B CONSTANT ESCAPE

\ Some words for controlling the terminal

\ Type a decimal number
: (DEC.) ( n -- ) 10 (BASE.) ;

\ Type the CSI sequence
: CSI ( -- ) ESCAPE EMIT [CHAR] [ EMIT ;

\ Type control characters to show the cursor
: SHOW-CURSOR ( -- ) CSI [CHAR] ? EMIT 25 (DEC.) [CHAR] h EMIT ;

\ Type control characters to hide the cursor
: HIDE-CURSOR ( -- ) CSI [CHAR] ? EMIT 25 (DEC.) [CHAR] l EMIT ;

\ Type control characters to save the cursor position
: SAVE-CURSOR ( -- ) CSI [CHAR] s EMIT ;

\ Type control characters to restore the cursor position
: RESTORE-CURSOR ( -- )  CSI [CHAR] u EMIT ;

\ Type control characters to erase to the end of the current line
: ERASE-END-OF-LINE ( -- ) CSI [CHAR] K EMIT ;

\ Type control characters to erase down from the current line
: ERASE-DOWN ( -- ) CSI [CHAR] J EMIT ;

\ Type control characters to go to a particular coordinate.
: GO-TO-COORD ( row column -- )
  SWAP CSI 1 + (DEC.) [CHAR] ; EMIT 1 + (DEC.) [CHAR] f EMIT ;

\ Initialize colors
: INIT-COLORS ( -- ) 0 0 0 0 CANVAS SET-COLOR 255 255 255 1 CANVAS SET-COLOR ;

\ Clear the world
: CLEAR-WORLD ( -- ) WORLD-0 WORLD-SIZE 0 FILL WORLD-1 WORLD-SIZE 0 FILL ;

\ Initialize world
: INIT-WORLD ( -- ) INIT-COLORS CLEAR-WORLD ;

INIT-WORLD

\ Get a cell at a coordinate
: CELL@ ( x y -- state ) WORLD-WIDTH * + CURRENT-WORLD @ + C@ ;

\ Set a cell at a coordinate
: CELL! ( state x y -- ) WORLD-WIDTH * + NEXT-WORLD @ + C! ;

\ Set a cell at a coordinate for the current world
: CELL-CURRENT! ( state x y -- ) WORLD-WIDTH * + CURRENT-WORLD @ + C! ;

\ Calculate an offset from a coordinate
: COORD+ ( x0 y0 x1 y1 -- x2 y2 ) ROT + ROT ROT + SWAP ;

\ Add a cell to a sum
: CELL+ ( n0 x y -- n1 ) CELL@ + ;

\ Set cell
: SET-CELL ( x y -- )
  1 2 PICK 2 PICK CELL!
  1 - SWAP 1 - SWAP 1 CANVAS PIXEL-NO-CLEAR! ;

\ Clear cell
: CLEAR-CELL ( x y -- )
  0 2 PICK 2 PICK CELL!
  1 - SWAP 1 - SWAP 0 CANVAS PIXEL-NO-CLEAR! ;

\ Set cell for the current world
: SET-CELL-CURRENT ( x y -- )
  1 2 PICK 2 PICK CELL-CURRENT!
  1 - SWAP 1 - SWAP 1 CANVAS PIXEL-NO-CLEAR! ;

\ Clear cell for the current world
: CLEAR-CELL-CURRENT ( x y -- )
  0 2 PICK 2 PICK CELL-CURRENT!
  1 - SWAP 1 - SWAP 0 CANVAS PIXEL-NO-CLEAR! ;

\ Update the canvas without cycling
: UPDATE-CANVAS ( -- )
  CANVAS CLEAR-PIXELS
  1 BEGIN DUP WORLD-WIDTH 1 - < WHILE
    1 BEGIN DUP WORLD-HEIGHT 1 - < wHILE
      2DUP CELL@ 2 PICK 1 - 2 PICK 1 - ROT CANVAS PIXEL-NO-CLEAR! 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP ;

\ Execute one cycle for a cell
: CYCLE-CELL ( x y -- )
  0 2 PICK 2 PICK -1 -1 COORD+ CELL+
  2 PICK 2 PICK -1 0 COORD+ CELL+
  2 PICK 2 PICK -1 1 COORD+ CELL+
  2 PICK 2 PICK 0 -1 COORD+ CELL+
  2 PICK 2 PICK 0 1 COORD+ CELL+
  2 PICK 2 PICK 1 -1 COORD+ CELL+
  2 PICK 2 PICK 1 0 COORD+ CELL+
  2 PICK 2 PICK 1 1 COORD+ CELL+
  2 PICK 2 PICK CELL@ IF
    DUP 2 = SWAP 3 = OR IF SET-CELL ELSE CLEAR-CELL THEN
  ELSE
    3 = IF SET-CELL ELSE CLEAR-CELL THEN
  THEN ;

\ Execute one cycle in the world
: CYCLE-WORLD ( -- )
  CANVAS CLEAR-PIXELS
  1 BEGIN DUP WORLD-WIDTH 1 - < WHILE
    1 BEGIN DUP WORLD-HEIGHT 1 - < WHILE
      2DUP CYCLE-CELL 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP
  NEXT-WORLD @ CURRENT-WORLD @ NEXT-WORLD ! CURRENT-WORLD ! ;

\ Display execution of cycles until key is pressed
: DISPLAY-CYCLES ( u -- )
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE SAVE-CURSOR
  BEGIN KEY? NOT WHILE
    RESTORE-CURSOR CYCLE-WORLD CANVAS DRAW
  REPEAT
  KEY? IF KEY DROP THEN
  SHOW-CURSOR 9999 0 GO-TO-COORD ;

\ Display execution of a specified number of cycles
: DISPLAY-N-CYCLES ( u -- )
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE SAVE-CURSOR
  BEGIN DUP 0 > WHILE
    RESTORE-CURSOR CYCLE-WORLD CANVAS DRAW 1 -
  REPEAT
  DROP
  SHOW-CURSOR 9999 0 GO-TO-COORD ;

\ Display the current state
: DISPLAY-CURRENT ( -- )
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE
  UPDATE-CANVAS CANVAS DRAW SHOW-CURSOR 9999 0 GO-TO-COORD ;

\ Get the next non-space character from a string, or null for end of string
: GET-CHAR ( addr1 bytes1 -- addr2 bytes2 c )
  BEGIN
    DUP 0 > IF
      OVER C@ DUP BL <> IF
        ROT 1 + ROT 1 - ROT TRUE
      ELSE
        DROP 1 - SWAP 1 + SWAP FALSE
      THEN
    ELSE
      0 TRUE
    THEN
  UNTIL ;

\ Convert a coordinate so it is relative to the center of the world
: CONVERT-COORD ( x0 y0 -- x1 y1 )
  WORLD-HEIGHT 2 / + SWAP WORLD-WIDTH 2 / + SWAP ;

\ Modify SET-CELL so that it is relative to the center of the world
: SET-CELL ( x y -- ) CONVERT-COORD SET-CELL-CURRENT ;

\ Modify CLEAR-CELL so that it is relative to the center of the world
: CLEAR-CELL ( x y -- ) CONVERT-COORD CLEAR-CELL-CURRENT ;

\ Modify CELL! so it uses SET-CELL and CLEAR-CELL
: CELL! ( state x y -- ) ROT IF SET-CELL ELSE CLEAR-CELL THEN ;

\ Modify CELL@ so that it is relative to the center of the world
: CELL@ ( x y -- state ) CONVERT-COORD CELL@ 0 <> ;

\ Set multiple cells with a string with the format "_" for an dead cell,
\ "*" for a live cell, and a "/" for a newline
: SET-MULTIPLE ( addr bytes x y -- )
  OVER >R 2SWAP BEGIN GET-CHAR DUP 0 <> WHILE
    CASE
      [CHAR] _ OF 2SWAP 2DUP CLEAR-CELL SWAP 1 + SWAP 2SWAP ENDOF
      [CHAR] * OF 2SWAP 2DUP SET-CELL SWAP 1 + SWAP 2SWAP ENDOF
      [CHAR] / OF 2SWAP 1 + NIP R@ SWAP 2SWAP ENDOF
    ENDCASE
  REPEAT
  DROP 2DROP 2DROP R> DROP ;

BASE ! SET-CURRENT SET-ORDER
