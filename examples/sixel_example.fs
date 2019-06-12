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

marker revert

include src/hashforth/sixel.fs

forth-wordlist task-wordlist sixel-wordlist 3 set-order
forth-wordlist set-current

512 constant canvas-width
512 constant canvas-height
2 constant red-color-count
2 constant green-color-count
2 constant blue-color-count
red-color-count green-color-count * blue-color-count * constant color-count

canvas-width canvas-height color-count new-sixel-fb constant canvas

$1b constant escape

: (dec.) ( n -- ) 10 (base.) ;

: csi ( -- ) escape emit [char] [ emit ;

: show-cursor ( -- ) csi [char] ? emit 25 (dec.) [char] h emit ;

: hide-cursor ( -- ) csi [char] ? emit 25 (dec.) [char] l emit ;

: save-cursor ( -- ) csi [char] s emit ;

: restore-cursor ( -- )  csi [char] u emit ;

: erase-end-of-line ( -- ) csi [char] K emit ;

: erase-down ( -- ) csi [char] J emit ;

: go-to-coord ( row column -- )
  swap csi 1 + (dec.) [char] ; emit 1 + (dec.) [char] f emit ;

: set-colors ( -- )
  0 begin dup red-color-count < while
    0 begin dup green-color-count < while
      0 begin dup blue-color-count < while
        ." (" 2 pick . over . dup (.) ." ) "
        2 pick 255 * red-color-count 1 - /
	2 pick 255 * green-color-count 1 - /
	2 pick 255 * blue-color-count 1 - /
	5 pick 2 lshift 5 pick 1 lshift or 4 pick or
	canvas set-color 1 +
      repeat
      drop 1 +
    repeat
    drop 1 +
  repeat
  drop ;

\ : FILL-CANVAS ( b -- ) CANVAS FILL-PIXELS ;

: fill-canvas ( b -- )
  >r canvas clear-pixels
  0 begin dup canvas-width < while
    0 begin dup canvas-height < while
      2dup 3 pick red-color-count * canvas-width / 2 lshift
      3 pick green-color-count * canvas-height / 1 lshift or r@ or
      canvas pixel-no-clear! 1 +
    repeat
    drop 1 +
  repeat
  drop r> drop ;

: animate ( -- )
\  TRUE SET-TRACE
  hide-cursor 0 0 go-to-coord erase-down erase-end-of-line save-cursor
  0 begin dup blue-color-count < key? not and while
    restore-cursor
    dup fill-canvas canvas draw ( 1 0 SLEEP ) 1 +
  repeat
  drop
  key? if key drop then
  show-cursor 9999 0 go-to-coord ;

set-colors animate

revert