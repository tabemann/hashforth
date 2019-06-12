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

get-order get-current base @

decimal
forth-wordlist 1 set-order
forth-wordlist set-current

include src/hashforth/sixel.fs

wordlist constant life-wordlist
forth-wordlist sixel-wordlist life-wordlist 3 set-order
life-wordlist set-current

\ Some basic constants
512 constant world-width
512 constant world-height
world-width world-height * constant world-size
2 constant color-count

\ Our canvas
world-width world-height color-count new-sixel-fb constant canvas

\ Our (double-buffered) forth world
create world-0 world-size allot
create world-1 world-size allot
variable current-world world-0 current-world !
variable next-world world-1 next-world !

\ The escape character
$1b constant escape

\ Some words for controlling the terminal

\ Type a decimal number
: (dec.) ( n -- ) 10 (base.) ;

\ Type the CSI sequence
: csi ( -- ) escape emit [char] [ emit ;

\ Type control characters to show the cursor
: show-cursor ( -- ) csi [char] ? emit 25 (dec.) [char] h emit ;

\ Type control characters to hide the cursor
: hide-cursor ( -- ) csi [char] ? emit 25 (dec.) [char] l emit ;

\ Type control characters to save the cursor position
: save-cursor ( -- ) csi [char] s emit ;

\ Type control characters to restore the cursor position
: restore-cursor ( -- )  csi [char] u emit ;

\ Type control characters to erase to the end of the current line
: erase-end-of-line ( -- ) csi [char] K emit ;

\ Type control characters to erase down from the current line
: erase-down ( -- ) csi [char] J emit ;

\ Type control characters to go to a particular coordinate.
: go-to-coord ( row column -- )
  swap csi 1 + (dec.) [char] ; emit 1 + (dec.) [char] f emit ;

\ Initialize colors
: init-colors ( -- ) 0 0 0 0 canvas set-color 255 255 255 1 canvas set-color ;

\ Clear the world
: clear-world ( -- ) world-0 world-size 0 fill world-1 world-size 0 fill ;

\ Initialize world
: init-world ( -- ) init-colors clear-world ;

init-world

\ Get a cell at a coordinate
: cell@ ( x y -- state ) [ world-width ] literal * + current-world @ + c@ ;

\ Get a cell at a coordinate, optimized for compilation
: ccell@ ( runtime: x y -- state )
  & (lit) world-width , & * & + & (lit) current-world , & @ & + & c@
; immediate compile-only

\ Get a cell at a coordinate in the next world
: cell-next@ ( x y -- state ) world-width * + next-world @ + c@ ;

\ Set a cell at a coordinate
: cell! ( state x y -- ) world-width * + next-world @ + c! ;

\ Set a cell at a coordinate for the current world
: cell-current! ( state x y -- ) world-width * + current-world @ + c! ;

\ Set cell
: set-cell ( x y -- )
  1 2 pick 2 pick cell! 1 [ canvas ] literal pixel-no-clear! ;

\ Clear cell
: clear-cell ( x y -- )
  0 2 pick 2 pick cell! 0 [ canvas ] literal pixel-no-clear! ;

\ Set cell for the current world
: set-cell-current ( x y -- )
  1 2 pick 2 pick cell-current! 1 [ canvas ] literal pixel-no-clear! ;

\ Clear cell for the current world
: clear-cell-current ( x y -- )
  0 2 pick 2 pick cell-current! 0 [ canvas ] literal pixel-no-clear! ;

\ Update the canvas without cycling
: update-canvas ( -- )
  [ canvas ] literal clear-pixels
  0 begin dup [ world-width ] literal < while
    0 begin dup [ world-height ] literal < while
      2dup cell@ 2 pick 1 - 2 pick 1 - rot [ canvas ] literal pixel-no-clear!
      1 +
    repeat
    drop 1 +
  repeat
  drop ;

\ Get the N neighbor of a cell
: n-neighbor ( x y -- c )
  1 - [ world-height 1 - ] literal over -1 > mux ;

\ Get the NE neighbor of a cell
: ne-neighbor ( x y -- c )
  1 - [ world-height 1 - ] literal over -1 > mux
  swap 1 + 0 over [ world-width ] literal < mux swap ;

\ Get the E neighbor of a cell
: e-neighbor ( x y -- c )
  swap 1 + 0 over [ world-width ] literal < mux swap ;

\ Get the SE neighbor of a cell
: se-neighbor ( x y -- c )
  1 + 0 over [ world-height ] literal < mux
  swap 1 + 0 over [ world-width ] literal < mux swap ;

\ Get the S neighbor of a cell
: s-neighbor ( x y -- c )
  1 + 0 over [ world-height ] literal < mux ;

\ Get the SW neighbor of a cell
: sw-neighbor ( x y -- c )
  1 + 0 over [ world-height ] literal < mux
  swap 1 - [ world-width 1 - ] literal over -1 > mux swap ;

\ Get the W neighbor of a cell
: w-neighbor ( x y -- c )
  swap 1 - [ world-width 1 - ] literal over -1 > mux swap ;

\ Get the NW neighbor of a cell
: nw-neighbor ( x y -- c )
  1 - [ world-height 1 - ] literal over -1 > mux
  swap 1 - [ world-width 1 - ] literal over -1 > mux swap ;

\ Execute one cycle for a cell
: cycle-cell ( x y -- )
  over over 1 - swap 1 - swap ccell@
  2 pick 2 pick 1 - ccell@ +
  2 pick 2 pick 1 - swap 1 + swap ccell@ +
  2 pick 2 pick swap 1 + swap ccell@ +
  2 pick 2 pick 1 + swap 1 + swap ccell@ +
  2 pick 2 pick 1 + ccell@ +
  2 pick 2 pick 1 + swap 1 - swap ccell@ +
  2 pick 2 pick swap 1 - swap ccell@ +
  2 pick 2 pick ccell@ if
    dup 2 = swap 3 = or if set-cell else clear-cell then
  else
    3 = if set-cell else clear-cell then
  then ;

\ Execute one cycle for an edge cell
: cycle-edge-cell ( x y -- )
  over over nw-neighbor ccell@
  2 pick 2 pick n-neighbor ccell@ +
  2 pick 2 pick ne-neighbor ccell@ +
  2 pick 2 pick e-neighbor ccell@ +
  2 pick 2 pick se-neighbor ccell@ +
  2 pick 2 pick s-neighbor ccell@ +
  2 pick 2 pick sw-neighbor ccell@ +
  2 pick 2 pick w-neighbor ccell@ +
  2 pick 2 pick ccell@ if
    dup 2 = swap 3 = or if set-cell else clear-cell then
  else
    3 = if set-cell else clear-cell then
  then ;

\ Execute one cycle in the world
: cycle-world ( -- )
  [ canvas ] literal clear-pixels
  1 begin dup [ world-width 1 - ] literal < while
    1 begin dup [ world-height 1 - ] literal < while
      2dup cycle-cell 1 +
    repeat
    drop 1 +
  repeat
  drop
  1 begin dup [ world-width 1 - ] literal < while
    dup 0 cycle-edge-cell dup [ world-height 1 - ] literal cycle-edge-cell 1 +
  repeat
  drop
  1 begin dup [ world-height 1 - ] literal < while
    0 over cycle-edge-cell [ world-width 1 - ] literal over cycle-edge-cell 1 +
  repeat
  drop
  0 0 cycle-edge-cell
  [ world-width 1 - ] literal 0 cycle-edge-cell
  [ world-width 1 - ] literal [ world-height 1 - ] literal cycle-edge-cell
  0 [ world-height 1 - ] literal cycle-edge-cell
  next-world @ current-world @ next-world ! current-world ! ;

\ Display execution of cycles until key is pressed
: display-cycles ( u -- )
  hide-cursor 0 0 go-to-coord erase-down erase-end-of-line save-cursor
  begin key? not while
    restore-cursor cycle-world [ canvas ] literal draw
  repeat
  key? if key drop then
  show-cursor 9999 0 go-to-coord ;

\ Display execution of a specified number of cycles
: display-n-cycles ( u -- )
  hide-cursor 0 0 go-to-coord erase-down erase-end-of-line save-cursor
  begin dup 0 > while
    restore-cursor cycle-world [ canvas ] literal draw 1 -
  repeat
  drop
  show-cursor 9999 0 go-to-coord ;

\ Display the current state
: display-current ( -- )
  hide-cursor 0 0 go-to-coord erase-down erase-end-of-line
  update-canvas [ canvas ] literal draw show-cursor 9999 0 go-to-coord ;

\ Get the next non-space character from a string, or null for end of string
: get-char ( addr1 bytes1 -- addr2 bytes2 c )
  begin
    dup 0 > if
      over c@ dup bl <> if
        rot 1 + rot 1 - rot true
      else
        drop 1 - swap 1 + swap false
      then
    else
      0 true
    then
  until ;

\ Convert a coordinate so it is relative to the center of the world
: convert-coord ( x0 y0 -- x1 y1 )
  world-height 2 / + swap world-width 2 / + swap ;

\ Modify SET-CELL so that it is relative to the center of the world
: set-cell ( x y -- ) convert-coord set-cell-current ;

\ Modify CLEAR-CELL so that it is relative to the center of the world
: clear-cell ( x y -- ) convert-coord clear-cell-current ;

\ Set a cell in the next world
: cell-next! ( state x y -- ) convert-coord cell! ;

\ Get a cell in the next world
: cell-next@ ( state x y -- ) convert-coord cell-next@ ;

\ Modify CELL! so it uses SET-CELL and CLEAR-CELL
: cell! ( state x y -- ) rot if set-cell else clear-cell then ;

\ Modify CELL@ so that it is relative to the center of the world
: cell@ ( x y -- state ) convert-coord cell@ 0 <> ;

\ Set multiple cells with a string with the format "_" for an dead cell,
\ "*" for a live cell, and a "/" for a newline
: set-multiple ( addr bytes x y -- )
  over >r 2swap begin get-char dup 0 <> while
    case
      [char] _ of 2swap 2dup clear-cell swap 1 + swap 2swap endof
      [char] * of 2swap 2dup set-cell swap 1 + swap 2swap endof
      [char] / of 2swap 1 + nip r@ swap 2swap endof
    endcase
  repeat
  drop 2drop 2drop r> drop ;

\ Flip part of a coordinate
: flip-coord-part ( n1 n-center -- n2 ) tuck - - ;

\ Get the center of part of a coordinate
: coord-part-center ( n1 n-span -- ) 2 / + ;

\ Copy cells from the next world back to the current world
: copy-next-to-current ( x y width height )
  3 pick begin dup 5 pick 4 pick + < while
    3 pick begin dup 5 pick 4 pick + < while
      2dup cell-next@ 2 pick 2 pick cell! 1 +
    repeat
    drop 1 +
  repeat
  drop 2drop 2drop ;

\ Actually flip a region horizontally
: do-flip-horizontal ( x y width height -- )
  3 pick 2 pick coord-part-center
  4 pick begin dup 6 pick 5 pick + < while
    4 pick begin dup 6 pick 5 pick + < while
      2dup cell@ 2 pick 4 pick flip-coord-part 2 pick cell-next! 1 +
    repeat
    drop 1 +
  repeat
  2drop 2drop 2drop ;

\ Actually flip a region vertically
: do-flip-vertical ( x y width height -- )
  2 pick over coord-part-center
  4 pick begin dup 6 pick 5 pick + < while
    4 pick begin dup 6 pick 5 pick + < while
      2dup cell@ 2 pick 2 pick 5 pick flip-coord-part cell-next! 1 +
    repeat
    drop 1 +
  repeat
  2drop 2drop 2drop ;

\ Flip a region horizontally
: flip-horizontal ( x y width height -- )
  3 pick 3 pick 3 pick 3 pick do-flip-horizontal copy-next-to-current ;

\ Flip a region vertically
: flip-vertical ( x y width height -- )
  3 pick 3 pick 3 pick 3 pick do-flip-vertical copy-next-to-current ;

\ Motion directions
0 constant ne
1 constant se
2 constant sw
3 constant nw

\ Flip a region in two dimensions
: flip-2d ( x y width height dir -- )
  case
    se of 2drop 2drop endof
    sw of flip-horizontal endof
    nw of 2over 2over flip-horizontal flip-vertical endof
    ne of flip-vertical endof
  endcase ;

\ Add a block to the world
: block ( x y -- ) s" ** / **" 2swap set-multiple ;

\ Add a blinker to the world (2 phases)
: blinker ( phase x y -- )
  rot case 0 of s" _*_ / _*_ / _*_" endof 1 of s" ___ / *** / ___" endof endcase
  2swap set-multiple ;

\ Add a glider to the world (4 phases)
: glider ( motion phase x y -- )
  rot case
    0 of s" _*_ / __* / ***" endof
    1 of s" *_* / _** / _*_" endof
    2 of s" __* / *_* / _**" endof
    3 of s" *__ / _** / **_" endof
  endcase
  2over set-multiple rot 3 3 rot flip-2d ;

\ Add an R-pentomino to the world
: r-pentomino ( dir x y -- )
  s" _** / **_ / _*_" 2over set-multiple rot 3 3 rot flip-2d ;

base ! set-current set-order
