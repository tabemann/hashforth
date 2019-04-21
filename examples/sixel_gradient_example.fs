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

FORTH-WORDLIST SIXEL-WORDLIST 2 SET-ORDER

16 CONSTANT COLOR-COUNT
16 CONSTANT COLOR-WIDTH
36 CONSTANT HEIGHT
COLOR-COUNT COLOR-WIDTH * HEIGHT COLOR-COUNT NEW-SIXEL-FB CONSTANT FB

: RED-GRADIENT ( index -- red ) COLOR-COUNT SWAP - 255 * COLOR-COUNT / ;

: GREEN-GRADIENT ( index -- green )
  DUP COLOR-COUNT 2 / < IF
    255 * COLOR-COUNT 2 / /
  ELSE
    COLOR-COUNT SWAP - 255 * COLOR-COUNT 2 / /
  THEN ;

: BLUE-GRADIENT ( index -- blue ) 255 * COLOR-COUNT / ;

: FILL-PALETTE ( -- )
  COLOR-COUNT 0 ?DO
    I RED-GRADIENT I GREEN-GRADIENT I BLUE-GRADIENT I FB SET-COLOR
  LOOP ;

: FILL-GRADIENT ( -- )
  FB CLEAR-PIXELS
  COLOR-COUNT 0 ?DO
    I COLOR-WIDTH * DUP COLOR-WIDTH + SWAP ?DO
      0 BEGIN DUP HEIGHT < WHILE I OVER J FB PIXEL-NO-CLEAR! 1 + REPEAT DROP
    LOOP
  LOOP ;

: DRAW-GRADIENT ( -- ) FILL-PALETTE FILL-GRADIENT FB DRAW ;

DRAW-GRADIENT
