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

INCLUDE src/hashforth/sixel.fs

FORTH-WORDLIST TASK-WORDLIST SIXEL-WORDLIST 3 SET-ORDER
FORTH-WORDLIST SET-CURRENT

128 CONSTANT CANVAS-WIDTH
128 CONSTANT CANVAS-HEIGHT
2 CONSTANT RED-COLOR-COUNT
2 CONSTANT GREEN-COLOR-COUNT
2 CONSTANT BLUE-COLOR-COUNT
RED-COLOR-COUNT GREEN-COLOR-COUNT * BLUE-COLOR-COUNT * CONSTANT COLOR-COUNT

CANVAS-WIDTH CANVAS-HEIGHT COLOR-COUNT NEW-SIXEL-FB CONSTANT CANVAS

$1B CONSTANT ESCAPE

: (DEC.) ( n -- ) 10 (BASE.) ;

: CSI ( -- ) ESCAPE EMIT [CHAR] [ EMIT ;

: SHOW-CURSOR ( -- ) CSI [CHAR] ? EMIT 25 (DEC.) [CHAR] h EMIT ;

: HIDE-CURSOR ( -- ) CSI [CHAR] ? EMIT 25 (DEC.) [CHAR] l EMIT ;

: SAVE-CURSOR ( -- ) CSI [CHAR] s EMIT ;

: RESTORE-CURSOR ( -- )  CSI [CHAR] u EMIT ;

: ERASE-END-OF-LINE ( -- ) CSI [CHAR] K EMIT ;

: ERASE-DOWN ( -- ) CSI [CHAR] J EMIT ;

: GO-TO-COORD ( row column -- )
  SWAP CSI 1 + (DEC.) [CHAR] ; EMIT 1 + (DEC.) [CHAR] f EMIT ;

: SET-COLORS ( -- )
  0 BEGIN DUP RED-COLOR-COUNT < WHILE
    0 BEGIN DUP GREEN-COLOR-COUNT < WHILE
      0 BEGIN DUP BLUE-COLOR-COUNT < WHILE
        ." (" 2 pick . over . dup (.) ." ) "
        2 PICK 255 * RED-COLOR-COUNT 1 - /
	2 PICK 255 * GREEN-COLOR-COUNT 1 - /
	2 PICK 255 * BLUE-COLOR-COUNT 1 - /
	5 PICK 2 LSHIFT 5 PICK 1 LSHIFT OR 4 PICK OR
	CANVAS SET-COLOR 1 +
      REPEAT
      DROP 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP ;

\ : FILL-CANVAS ( b -- ) CANVAS FILL-PIXELS ;

: FILL-CANVAS ( b -- )
  >R CANVAS CLEAR-PIXELS
  0 BEGIN DUP CANVAS-WIDTH < WHILE
    0 BEGIN DUP CANVAS-HEIGHT < WHILE
      2DUP 3 PICK RED-COLOR-COUNT * CANVAS-WIDTH / 2 LSHIFT
      3 PICK GREEN-COLOR-COUNT * CANVAS-HEIGHT / 1 LSHIFT OR R@ OR
      CANVAS PIXEL-NO-CLEAR! 1 +
    REPEAT
    DROP 1 +
  REPEAT
  DROP R> DROP ;

: ANIMATE ( -- )
\  TRUE SET-TRACE
  HIDE-CURSOR 0 0 GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE SAVE-CURSOR
  0 BEGIN DUP BLUE-COLOR-COUNT < KEY? NOT AND WHILE
    RESTORE-CURSOR
    DUP FILL-CANVAS CANVAS DRAW 1 0 SLEEP 1 +
  REPEAT
  DROP
  KEY? IF KEY DROP THEN
  SHOW-CURSOR 9999 0 GO-TO-COORD ;

SET-COLORS ANIMATE