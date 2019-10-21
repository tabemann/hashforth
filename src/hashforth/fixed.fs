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

wordlist constant fixed-wordlist
forth-wordlist lambda-wordlist fixed-wordlist 3 set-order
fixed-wordlist set-current

\ Invalid precision exception
: x-invalid-precision ( -- ) space ." invalid precision" cr ;

\ High precision in bits
user high-precision-bits
0 high-precision-bits !

\ Low precision in bits
user low-precision-bits
0 low-precision-bits !

\ Default high precision in bits
variable default-high-precision-bits
16 default-high-precision-bits !

\ Default low precision in bits
variable default-low-precision-bits
16 default-low-precision-bits !

\ Set precision
: set-precision ( high-bits low-bits -- )
  2dup + cell 8 * <= averts x-invalid-precision
  over 1 >= averts x-invalid-precision
  dup 0 >= averts x-invalid-precision
  low-precision-bits ! high-precision-bits ! ;

\ Set default precision
: set-default-precision ( high-bits low-bits -- )
  2dup + cell 8 * <= averts x-invalid-precision
  over 1 >= averts x-invalid-precision
  dup 0 >= averts x-invalid-precision
  default-low-precision-bits ! default-high-precision-bits ! ;

\ Initialize precision
: init-precision ( -- )
  high-precision-bits @ 0 = if
    default-high-precision-bits @ high-precision-bits !
    default-low-precision-bits @ low-precision-bits !
  then ;

\ Invalid number exception
: x-invalid-number ( -- ) space ." invalid number" cr ;

\ Convert a signed integer into a fixed point number
: s>f ( n -- f )
  init-precision
  dup 0 > if
    dup 1 high-precision-bits @ 1 - lshift u< averts x-invalid-number
    low-precision-bits @ lshift
  else
    negate dup 1 high-precision-bits @ 1 - lshift u<= averts x-invalid-number
    low-precision-bits @ lshift negate
  then ;

\ Convert a fixed point number into a signed integer to the right of the
\ decimal point, rounding to zero
: f>s ( f -- n ) init-precision 1 low-precision-bits @ lshift / ;

\ Convert a fixed point number into a signed integer to the right of the
\ decdimal point, rounding down
: f>s-down ( f -- n ) init-precision low-precision-bits @ arshift ;

\ Get the floor of a fixed point number
: floor ( f -- f ) init-precision f>s-down s>f ;

\ Get the portion of a fixed point number right of the decimal point
: fraction ( f -- f ) 1 low-precision-bits @ lshift 1 - and ;

\ Get the ceiling of a fixed point number
: ceiling ( f -- f )
  init-precision dup f>s-down s>f tuck - 0 > if
    1 low-precision-bits @ lshift +
  then ;

\ Multiply two fixed point numbers
: f* ( f1 f2 -- f3 ) init-precision low-precision-bits @ *rshift ;

\ Divide two fixed point numbers
: f/ ( f1 f2 -- f3 ) init-precision 1 low-precision-bits @ lshift swap */ ;

\ Multiply two unsigned fixed point numbers
: fu* ( f1 f2 -- f3 ) init-precision low-precision-bits @ u*rshift ;

\ Divide two unsigned fixed point numbers
: fu/ ( f1 f2 -- f3 ) init-precision low-precision-bits @ swap ulshift/ ;

\ Get the decimal point index for a fixed point number in a string
: parse-fixed-point ( c-addr bytes - index matches )
  0 >r begin
    dup 0 u> if
      over c@ [char] . = if true else 1 - swap 1 + swap r> 1 + >r false then
    else
      true
    then
  until
  0 = if r> 2drop 0 false else drop r> true then ;

\ Parse a fixed point number after the decimal point
: parse-fixed-after ( base c-addr bytes -- n matches )
  rot >r 0 begin
    over 0 > if
      rot rot 1 - 2dup + c@ r@ swap parse-digit if
	low-precision-bits @ lshift 3 roll r@
	low-precision-bits @ lshift fu/ + false
      else
	2drop 2drop r> drop 0 false true
      then
    else
      rot rot 2drop r> low-precision-bits @ lshift fu/ true true
    then
  until ;

\ Main body of parsing a fixed point number
: parse-fixed-main ( c-addr bytes base -- f matches )
  over 0 u> if
    >r 2dup parse-fixed-point if
      r@ 3 pick 2 pick parse-digits if
	dup 1 high-precision-bits @ lshift u< if
	  low-precision-bits @ lshift r> 4 roll 3 pick + 1 + 4 roll 4 roll - 1 -
	  dup 0 u> if
	    parse-fixed-after if
	      or true
	    else
	      2drop 0 false
	    then
	  else
	    2drop 0 false
	  then
	else
	  2drop 2drop r> drop 0 false
	then
      else
	2drop 2drop r> drop 0 false
      then
    else
      drop 2drop r> drop 0 false
    then
  else
    drop 2drop 0 false
  then ;

\ Parse a fixed point number
: parse-fixed ( c-addr bytes -- f matches )
  init-precision
  parse-base over 0 u> if
    2 pick c@ [char] - = if
      rot 1 + rot 1 - rot parse-fixed-main dup if swap negate swap then
    else
      parse-fixed-main
    then
  else
    2drop drop 0 false
  then ;

\ Number parser
: parse-compile-number-or-double-or-fixed ( c-addr bytes -- x matches )
  2dup 2>r parse-compile-number-or-double if
    2r> 2drop true
  else
    2r> parse-fixed if
      state @ if lit, then true
    else
      drop false
    then
  then ;

\ Set number parser
' parse-compile-number-or-double-or-fixed 'handle-number !

\ Format digits to the right of the decimal point
: format-fraction ( f -- c-addr )
  fraction dup 0 > if
    0 begin ( fraction index )
      over 0 > if
	swap base @ * tuck f>s dup 10 < if
	  [char] 0 +
	else
	  10 - [char] A +
	then ( fraction index char )
	format-digit-buffer @ 2 pick + c! ( fraction index )
	swap fraction swap 1 + false
      else
	true
      then
    until
    nip
  else
    drop [char] 0 format-digit-buffer @ c! 1
  then
  format-digit-buffer @ swap format-digit-count over -
  format-digit-buffer @ + dup >r swap move r> ;

\ Actually format an unsigned fixed point number
: (format-fixed-unsigned) ( f -- c-addr bytes )
  dup format-fraction 1 - [char] . over c!
  swap f>s dup 0 u> if
    begin
      dup 0 u>
    while
      dup base @ umod dup 10 u< if
	[char] 0 +
      else
	10 - [char] A +
      then
      rot 1 - tuck c! swap base @ u/
    repeat
    drop
  else
    drop 1 - [char] 0 over c!
  then
  complete-format-digit-buffer ;

\ Format a fixed point number
: format-fixed ( f -- c-addr bytes )
  init-precision
  dup 0 >= if
   (format-fixed-unsigned)
  else
    negate (format-fixed-unsigned) 1 + swap 1 - [char] - over c! swap
  then ;

\ Format an unsigned fixed point number
: format-fixed-unsigned ( f -- c-addr bytes )
  init-precision (format-fixed-unsigned) ;

\ Output a signed fixed point number on standard output with no following space
: (f.) ( f -- ) format-fixed type ;

\ Output an unsigned fixed point number on standard output with no following
\ space
: (fu.) ( f -- ) format-fixed-unsigned type ;

\ Output a signed fixed point number on standard output with a following space
: f. ( f -- ) (f.) space ;

\ Output an unsigned fixed point number on standard output with a following
\ space
: fu. ( f -- ) (fu.) space ;

base ! set-current set-order
