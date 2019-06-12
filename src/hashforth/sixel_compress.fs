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

wordlist constant sixel-wordlist
forth-wordlist sixel-wordlist 2 set-order
sixel-wordlist set-current

1024 constant sixel-output-size

$1b constant escape
\ BL CONSTANT ESCAPE

begin-structure sixel-fb-size
  field: sixel-fb-width
  field: sixel-fb-height
  field: sixel-fb-colors
  field: sixel-fb-color-count
  field: sixel-fb-row-length
  field: sixel-fb-color-length
  field: sixel-fb-data
  field: sixel-fb-data-current
  field: sixel-fb-data-end
  field: sixel-fb-compress
  field: sixel-fb-compress-current
  field: sixel-fb-output
  field: sixel-fb-output-index
end-structure

1024 constant max-num-count
1024 constant max-rle-multiplier-count

create prefixed-num-sizes max-num-count allot
create prefixed-num-sums max-num-count 2 * allot
create rle-multipliers max-rle-multiplier-count 5 * allot

: init-prefixed-num-sums ( -- )
  0 0 begin dup max-num-count < while
    dup dup 10 < if
      drop 2
    else dup 100 < if
      drop 3
    else 1000 < if
      4
    else
      5
    then then then
    2dup swap prefixed-num-sizes + c! rot + 2dup swap 2 *
    prefixed-num-sums + h! swap 1 +
  repeat
  2drop ;

init-prefixed-num-sums

: init-rle-multipliers ( -- )
  0 begin dup max-rle-multiplier-count < while
    [char] ! over 5 * rle-multipliers + c!
    dup format-number 2 pick 5 * 1 + rle-multipliers + swap move
    1 +
  repeat
  drop ;

init-rle-multipliers

: buffer-char ( c fb -- )
  dup sixel-fb-output-index @ sixel-output-size >= if
    dup sixel-fb-output @ sixel-output-size type
    1 over sixel-fb-output-index ! sixel-fb-output @ c!
  else
    dup sixel-fb-output-index @ >r tuck sixel-fb-output @ r@ + c!
    r> 1 + swap sixel-fb-output-index !
  then ;

: buffer-string ( c-addr u fb -- )
  >r begin dup 0 > while
    1 - swap dup c@ r@ buffer-char 1 + swap
  repeat
  r> drop 2drop ;

: buffer-decimal ( n fb -- )
  swap ['] format-number 10 base-execute rot buffer-string ;

: flush-buffer ( fb -- )
  dup sixel-fb-output @ over sixel-fb-output-index @ type
  0 swap sixel-fb-output-index ! ;

: begin-frame ( fb -- )
  escape over buffer-char [char] P over buffer-char [char] q swap buffer-char ;

: end-frame ( -- ) escape emit [char] \ emit ;

: clear-colors ( fb -- )
  >r 0 begin dup r@ sixel-fb-color-count @ < while
    dup cells r@ sixel-fb-colors @ + 0 swap ! 1 +
  repeat
  drop r> drop ;

: set-color ( r g b color fb -- )
  sixel-fb-colors @ swap cells +
  swap $ff and rot $ff and 8 lshift or rot $ff and 16 lshift or swap ! ;

: get-color ( color fb -- r g b )
  sixel-fb-colors @ swap cells + @ >r
  r@ 16 rshift r@ 8 rshift $ff and r> $ff and ;

: generate-palette-entry ( r g b color fb --)
  [char] # over buffer-char tuck buffer-decimal
  [char] ; over buffer-char [char] 2 over buffer-char
  [char] ; over buffer-char 3 roll 100 * 255 / over buffer-decimal
  [char] ; over buffer-char rot 100 * 255 / over buffer-decimal
  [char] ; over buffer-char swap 100 * 255 / swap buffer-decimal ;

: generate-palette ( fb -- )
  >r 0 begin dup r@ sixel-fb-color-count @ < while
    dup r@ get-color 3 pick r@ generate-palette-entry 1 +
  repeat
  drop r> drop ;

: get-data-size ( fb -- bytes )
  >r r@ sixel-fb-color-count @ r@ sixel-fb-width @ 1 + *
  r@ sixel-fb-height @ 6 / *
  r@ sixel-fb-height @ 6 /
  r@ sixel-fb-color-count @ 1 - 2 * prefixed-num-sums + h@ * +
  r> sixel-fb-height @ 6 / + ;

: get-row-length ( fb -- bytes )
  >r r@ sixel-fb-color-count @ r@ sixel-fb-width @ 1 + * 1 +
  r> sixel-fb-color-count @ 1 - 2 * prefixed-num-sums + h@ + ;

: get-color-length ( fb -- bytes ) sixel-fb-width @ 1 + ;

: get-address ( x y color fb -- addr )
  >r r@ sixel-fb-row-length @ rot 6 / *
  over 2 * prefixed-num-sums + h@ +
  swap r@ sixel-fb-color-length @ * + + r> sixel-fb-data @ + ;

: clear-pixel ( x y fb -- )
  >r r@ sixel-fb-row-length @ over 6 / * r@ sixel-fb-data @ + rot +
  swap 6 mod swap
  0 begin dup r@ sixel-fb-color-count @ < while
    swap over prefixed-num-sizes + c@ + swap
    over c@ 63 - 1 3 pick lshift not and 63 + 2 pick c!
    1 + swap r@ sixel-fb-color-length @ + swap
  repeat
  2drop drop r> drop ;

: pixel! ( x y color fb -- )
  3 pick 3 pick 2 pick clear-pixel
  2 pick >r get-address dup c@ 63 - 1 r> 6 mod lshift or 63 + swap c! ;

: pixel-no-clear! ( x y color fb -- )
  2 pick >r get-address dup c@ 63 - 1 r> 6 mod lshift or 63 + swap c! ;

: populate-lf ( y/6 fb -- )
  >r r@ sixel-fb-row-length @ swap 1 + * 1 - r> sixel-fb-data @ +
  [char] - swap c! ;

: populate-cr ( y/6 color fb -- )
  >r dup 2 * prefixed-num-sums + h@
  r@ sixel-fb-row-length @ 3 pick * + over r@ sixel-fb-color-length @ * +
  r@ sixel-fb-width @ + r> sixel-fb-data @ + [char] $ swap c! 2drop ;

: populate-color ( y/6 color fb -- )
  >r dup 2 * prefixed-num-sums + h@ over prefixed-num-sizes + c@ -
  r@ sixel-fb-row-length @ 3 pick * + over r@ sixel-fb-color-length @ * +
  r> sixel-fb-data @ + [char] # over c! 1 +
  over ['] format-number 10 base-execute rot swap move 2drop ;

: clear-line-color ( y/6 color fb -- )
  >r dup 2 * prefixed-num-sums + h@
  r@ sixel-fb-row-length @ 3 pick * + over r@ sixel-fb-color-length @ * +
  r@ sixel-fb-data @ +
  r> sixel-fb-width @ 63 fill 2drop ;

: fill-line-color ( y/6 color fb -- )
  >r dup 2 * prefixed-num-sums + h@
  r@ sixel-fb-row-length @ 3 pick * + over r@ sixel-fb-color-length @ * +
  r@ sixel-fb-data @ +
  r> sixel-fb-width @ 126 fill 2drop ;

: clear-pixels ( fb -- )
  >r 0 begin dup r@ sixel-fb-height @ 6 / < while
    0 begin dup r@ sixel-fb-color-count @ < while
      2dup r@ clear-line-color 1 +
    repeat
    drop 1 +
  repeat
  drop r> drop ;

: fill-pixels ( color fb -- )
  >r 0 begin dup r@ sixel-fb-height @ 6 / < while
    0 begin dup r@ sixel-fb-color-count @ < while
      dup 3 pick = if
        2dup r@ fill-line-color
      else
        2dup r@ clear-line-color
      then
      1 +
    repeat
    drop 1 +
  repeat
  2drop r> drop ;

: init-pixels ( fb -- )
  >r 0 begin dup r@ sixel-fb-height @ 6 / < while
    dup r@ populate-lf
    0 begin dup r@ sixel-fb-color-count @ < while
      2dup r@ populate-cr
      2dup r@ populate-color
      2dup r@ clear-line-color
      1 +
    repeat
    drop 1 +
  repeat
  drop r> drop ;

: get-run-length ( fb -- )
  sixel-fb-data-current @ dup c@ >r 1 + 1 begin
    dup max-num-count < if
      over c@ r@ = if
        swap 1 + swap 1 + false
      else
        true
      then
    else
      true
    then
  until
  nip r> drop ;

: compress ( fb -- )
  >r begin r@ sixel-fb-data-current @ r@ sixel-fb-data-end @ < while
    r@ sixel-fb-data-current @ c@ dup 63 < over 126 > or if
      r@ sixel-fb-compress-current @ c!
      1 r@ sixel-fb-data-current +!
      1 r@ sixel-fb-compress-current +!
    else
      r@ get-run-length dup prefixed-num-sizes + c@ 1 + over < if
        dup 5 * rle-multipliers + r@ sixel-fb-compress-current @
	2 pick prefixed-num-sizes + c@ dup >r cmove
	r> r@ sixel-fb-compress-current +!
	r@ sixel-fb-data-current +!
	r@ sixel-fb-compress-current @ c!
	1 r@ sixel-fb-compress-current +!
      else
        r@ sixel-fb-compress-current @ over 3 roll fill
	dup r@ sixel-fb-data-current +!
	r@ sixel-fb-compress-current +!
      then
    then
  repeat
  r> drop ;

: draw ( fb -- )
  dup begin-frame dup generate-palette dup flush-buffer
  dup sixel-fb-data @ over sixel-fb-data-current !
  dup sixel-fb-compress @ over sixel-fb-compress-current !
  dup compress
  dup sixel-fb-compress @ swap sixel-fb-compress-current @ over - type
  end-frame ;

: new-sixel-fb ( width height colors -- fb )
  >r dup 6 mod dup 0 > if 6 swap - + else drop then
  here sixel-fb-size allot
  r> over sixel-fb-color-count ! tuck sixel-fb-height ! tuck sixel-fb-width !
  dup get-row-length over sixel-fb-row-length !
  dup get-color-length over sixel-fb-color-length !
  here >r dup sixel-fb-color-count @ cells allot r> over sixel-fb-colors !
  dup clear-colors
  here >r dup get-data-size allot r> over sixel-fb-data !
  dup sixel-fb-data @ over sixel-fb-data-current !
  dup sixel-fb-data @ over get-data-size + over sixel-fb-data-end !
  dup init-pixels
  here >r dup get-data-size allot r> over sixel-fb-compress !
  dup sixel-fb-compress @ over sixel-fb-compress-current !
  here >r sixel-output-size allot r> over sixel-fb-output !
  0 over sixel-fb-output-index ! ;

base ! set-current set-order