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
WORLD-WIDTH WORLD-HEIGHT COLOR-COUNT NEW-SIXEL-FB CONSTANT CANVAS

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
: CELL@ ( x y -- state ) [ WORLD-WIDTH ] LITERAL * + CURRENT-WORLD @ + C@ ;

\ Get a cell at a coordinate, optimized for compilation
: CCELL@ ( runtime: x y -- state )
  & (LIT) WORLD-WIDTH , & * & + & (LIT) CURRENT-WORLD , & @ & + & C@
; IMMEDIATE COMPILE-ONLY

\ Get a cell at a coordinate in the next world
: CELL-NEXT@ ( x y -- state ) WORLD-WIDTH * + NEXT-WORLD @ + C@ ;

\ Set a cell at a coordinate
: CELL! ( state x y -- ) WORLD-WIDTH * + NEXT-WORLD @ + C! ;

\ Set a cell at a coordinate for the current world
: CELL-CURRENT! ( state x y -- ) WORLD-WIDTH * + CURRENT-WORLD @ + C! ;

\ Set cell
: SET-CELL ( x y -- )
  1 2 PICK 2 PICK CELL! 1 [ CANVAS ] LITERAL PIXEL-NO-CLEAR! ;

\ Clear cell
: CLEAR-CELL ( x y -- )
  0 2 PICK 2 PICK CELL! 0 [ CANVAS ] LITERAL PIXEL-NO-CLEAR! ;

\ Set cell for the current world
: SET-CELL-CURRENT ( x y -- )
  1 2 PICK 2 PICK CELL-CURRENT! 1 [ CANVAS ] LITERAL PIXEL-NO-CLEAR! ;

\ Clear cell for the current world
: CLEAR-CELL-CURRENT ( x y -- )
  0 2 PICK 2 PICK CELL-CURRENT! 0 [ CANVAS ] LITERAL PIXEL-NO-CLEAR! ;

\ Update the canvas without cycling
: UPDATE-CANVAS ( -- )
  [ CANVAS ] LITERAL CLEAR-PIXELS
  0 BEGIN DUP [ WORLD-WIDTH ] LITERAL < WHILE
    0 BEGIN DUP [ WORLD-HEIGHT ] LITERAL < WHILE
      2DUP CELL@ 2 PICK 1 - 2 PICK 1 - ROT [ CANVAS ] LITERAL PIXEL-NO-CLEAR!
      1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP ;

\ Get the N neighbor of a cell
: N-NEIGHBOR ( x y -- c )
  1 - [ WORLD-HEIGHT 1 - ] LITERAL OVER -1 > MUX ;

\ Get the NE neighbor of a cell
: NE-NEIGHBOR ( x y -- c )
  1 - [ WORLD-HEIGHT 1 - ] LITERAL OVER -1 > MUX
  SWAP 1 + 0 OVER [ WORLD-WIDTH ] LITERAL < MUX SWAP ;

\ Get the E neighbor of a cell
: E-NEIGHBOR ( x y -- c )
  SWAP 1 + 0 OVER [ WORLD-WIDTH ] LITERAL < MUX SWAP ;

\ Get the SE neighbor of a cell
: SE-NEIGHBOR ( x y -- c )
  1 + 0 OVER [ WORLD-HEIGHT ] LITERAL < MUX
  SWAP 1 + 0 OVER [ WORLD-WIDTH ] LITERAL < MUX SWAP ;

\ Get the S neighbor of a cell
: S-NEIGHBOR ( x y -- c )
  1 + 0 OVER [ WORLD-HEIGHT ] LITERAL < MUX ;

\ Get the SW neighbor of a cell
: SW-NEIGHBOR ( x y -- c )
  1 + 0 OVER [ WORLD-HEIGHT ] LITERAL < MUX
  SWAP 1 - [ WORLD-WIDTH 1 - ] LITERAL OVER -1 > MUX SWAP ;

\ Get the W neighbor of a cell
: W-NEIGHBOR ( x y -- c )
  SWAP 1 - [ WORLD-WIDTH 1 - ] LITERAL OVER -1 > MUX SWAP ;

\ Get the NW neighbor of a cell
: NW-NEIGHBOR ( x y -- c )
  1 - [ WORLD-HEIGHT 1 - ] LITERAL OVER -1 > MUX
  SWAP 1 - [ WORLD-WIDTH 1 - ] LITERAL OVER -1 > MUX SWAP ;

\ Execute one cycle for a cell
: CYCLE-CELL ( x y -- )
  OVER OVER 1 - SWAP 1 - SWAP CCELL@
  2 PICK 2 PICK 1 - CCELL@ +
  2 PICK 2 PICK 1 - SWAP 1 + SWAP CCELL@ +
  2 PICK 2 PICK SWAP 1 + SWAP CCELL@ +
  2 PICK 2 PICK 1 + SWAP 1 + SWAP CCELL@ +
  2 PICK 2 PICK 1 + CCELL@ +
  2 PICK 2 PICK 1 + SWAP 1 - SWAP CCELL@ +
  2 PICK 2 PICK SWAP 1 - SWAP CCELL@ +
  2 PICK 2 PICK CCELL@ IF
    DUP 2 = SWAP 3 = OR IF SET-CELL ELSE CLEAR-CELL THEN
  ELSE
    3 = IF SET-CELL ELSE CLEAR-CELL THEN
  THEN ;

\ Execute one cycle for an edge cell
: CYCLE-EDGE-CELL ( x y -- )
  OVER OVER NW-NEIGHBOR CCELL@
  2 PICK 2 PICK N-NEIGHBOR CCELL@ +
  2 PICK 2 PICK NE-NEIGHBOR CCELL@ +
  2 PICK 2 PICK E-NEIGHBOR CCELL@ +
  2 PICK 2 PICK SE-NEIGHBOR CCELL@ +
  2 PICK 2 PICK S-NEIGHBOR CCELL@ +
  2 PICK 2 PICK SW-NEIGHBOR CCELL@ +
  2 PICK 2 PICK W-NEIGHBOR CCELL@ +
  2 PICK 2 PICK CCELL@ IF
    DUP 2 = SWAP 3 = OR IF SET-CELL ELSE CLEAR-CELL THEN
  ELSE
    3 = IF SET-CELL ELSE CLEAR-CELL THEN
  THEN ;

\ Execute one cycle in the world
: CYCLE-WORLD ( -- )
  [ CANVAS ] LITERAL CLEAR-PIXELS
  1 BEGIN DUP [ WORLD-WIDTH 1 - ] LITERAL < WHILE
    1 BEGIN DUP [ WORLD-HEIGHT 1 - ] LITERAL < WHILE
      2DUP CYCLE-CELL 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP
  1 BEGIN DUP [ WORLD-WIDTH 1 - ] LITERAL < WHILE
    DUP 0 CYCLE-EDGE-CELL DUP [ WORLD-HEIGHT 1 - ] LITERAL CYCLE-EDGE-CELL 1 +
  REPEAT
  DROP
  1 BEGIN DUP [ WORLD-HEIGHT 1 - ] LITERAL < WHILE
    0 OVER CYCLE-EDGE-CELL [ WORLD-WIDTH 1 - ] LITERAL OVER CYCLE-EDGE-CELL 1 +
  REPEAT
  DROP
  0 0 CYCLE-EDGE-CELL
  [ WORLD-WIDTH 1 - ] LITERAL 0 CYCLE-EDGE-CELL
  [ WORLD-WIDTH 1 - ] LITERAL [ WORLD-HEIGHT 1 - ] LITERAL CYCLE-EDGE-CELL
  0 [ WORLD-HEIGHT 1 - ] LITERAL CYCLE-EDGE-CELL
  NEXT-WORLD @ CURRENT-WORLD @ NEXT-WORLD ! CURRENT-WORLD ! ;

\ Display execution of cycles until key is pressed
: DISPLAY-CYCLES ( u -- )
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE SAVE-CURSOR
  BEGIN KEY? NOT WHILE
    RESTORE-CURSOR CYCLE-WORLD [ CANVAS ] LITERAL DRAW
  REPEAT
  KEY? IF KEY DROP THEN
  SHOW-CURSOR 9999 0 GO-TO-COORD ;

\ Display execution of a specified number of cycles
: DISPLAY-N-CYCLES ( u -- )
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE SAVE-CURSOR
  BEGIN DUP 0 > WHILE
    RESTORE-CURSOR CYCLE-WORLD [ CANVAS ] LITERAL DRAW 1 -
  REPEAT
  DROP
  SHOW-CURSOR 9999 0 GO-TO-COORD ;

\ Display the current state
: DISPLAY-CURRENT ( -- )
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE
  UPDATE-CANVAS [ CANVAS ] LITERAL DRAW SHOW-CURSOR 9999 0 GO-TO-COORD ;

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

\ Set a cell in the next world
: CELL-NEXT! ( state x y -- ) CONVERT-COORD CELL! ;

\ Get a cell in the next world
: CELL-NEXT@ ( state x y -- ) CONVERT-COORD CELL-NEXT@ ;

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

\ Flip part of a coordinate
: FLIP-COORD-PART ( n1 n-center -- n2 ) TUCK - - ;

\ Get the center of part of a coordinate
: COORD-PART-CENTER ( n1 n-span -- ) 2 / + ;

\ Copy cells from the next world back to the current world
: COPY-NEXT-TO-CURRENT ( x y width height )
  3 PICK BEGIN DUP 5 PICK 4 PICK + < WHILE
    3 PICK BEGIN DUP 5 PICK 4 PICK + < WHILE
      2DUP CELL-NEXT@ 2 PICK 2 PICK CELL! 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP 2DROP 2DROP ;

\ Actually flip a region horizontally
: DO-FLIP-HORIZONTAL ( x y width height -- )
  3 PICK 2 PICK COORD-PART-CENTER
  4 PICK BEGIN DUP 6 PICK 5 PICK + < WHILE
    4 PICK BEGIN DUP 6 PICK 5 PICK + < WHILE
      2DUP CELL@ 2 PICK 4 PICK FLIP-COORD-PART 2 PICK CELL-NEXT! 1 +
    REPEAT
    DROP 1 +
  REPEAT
  2DROP 2DROP 2DROP ;

\ Actually flip a region vertically
: DO-FLIP-VERTICAL ( x y width height -- )
  2 PICK OVER COORD-PART-CENTER
  4 PICK BEGIN DUP 6 PICK 5 PICK + < WHILE
    4 PICK BEGIN DUP 6 PICK 5 PICK + < WHILE
      2DUP CELL@ 2 PICK 2 PICK 5 PICK FLIP-COORD-PART CELL-NEXT! 1 +
    REPEAT
    DROP 1 +
  REPEAT
  2DROP 2DROP 2DROP ;

\ Flip a region horizontally
: FLIP-HORIZONTAL ( x y width height -- )
  3 PICK 3 PICK 3 PICK 3 PICK DO-FLIP-HORIZONTAL COPY-NEXT-TO-CURRENT ;

\ Flip a region vertically
: FLIP-VERTICAL ( x y width height -- )
  3 PICK 3 PICK 3 PICK 3 PICK DO-FLIP-VERTICAL COPY-NEXT-TO-CURRENT ;

\ Motion directions
0 CONSTANT NE
1 CONSTANT SE
2 CONSTANT SW
3 CONSTANT NW

\ Flip a region in two dimensions
: FLIP-2D ( x y width height dir -- )
  CASE
    SE OF 2DROP 2DROP ENDOF
    SW OF FLIP-HORIZONTAL ENDOF
    NW OF 2OVER 2OVER FLIP-HORIZONTAL FLIP-VERTICAL ENDOF
    NE OF FLIP-VERTICAL ENDOF
  ENDCASE ;

\ Add a block to the world
: BLOCK ( x y -- ) S" ** / **" 2SWAP SET-MULTIPLE ;

\ Add a blinker to the world (2 phases)
: BLINKER ( phase x y -- )
  ROT CASE 0 OF S" _*_ / _*_ / _*_" ENDOF 1 OF S" ___ / *** / ___" ENDOF ENDCASE
  2SWAP SET-MULTIPLE ;

\ Add a glider to the world (4 phases)
: GLIDER ( motion phase x y -- )
  ROT CASE
    0 OF S" _*_ / __* / ***" ENDOF
    1 OF S" *_* / _** / _*_" ENDOF
    2 OF S" __* / *_* / _**" ENDOF
    3 OF S" *__ / _** / **_" ENDOF
  ENDCASE
  2OVER SET-MULTIPLE ROT 3 3 ROT FLIP-2D ;

\ Add an R-pentomino to the world
: R-PENTOMINO ( dir x y -- )
  S" _** / **_ / _*_" 2OVER SET-MULTIPLE ROT 3 3 ROT FLIP-2D ;

BASE ! SET-CURRENT SET-ORDER
