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
\ decimal point
: f>s ( f -- n ) init-precision low-precision-bits @ arshift ;

\ Get the floor of a fixed point number
: floor ( f -- f ) init-precision f>s s>f ;

\ Get the portion of a fixed point number right of the decimal point
: fraction ( f -- f ) dup floor - ;

\ Get the ceiling of a fixed point number
: ceiling ( f -- f )
  init-precision dup f>s s>f tuck - 0 > if
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

base ! set-current set-order
