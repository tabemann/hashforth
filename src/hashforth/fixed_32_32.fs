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

cell 8 = [if]
  
  wordlist constant fixed-32.32-wordlist
  forth-wordlist lambda-wordlist fixed-32.32-wordlist 3 set-order
  fixed-32.32-wordlist set-current
  
  \ Invalid number exception
  : x-invalid-number ( -- ) space ." invalid number" cr ;
  
  \ Convert a signed integer into a 32.32 fixed point number
  : s>f ( n -- f32.32 )
    dup 0 > if
      dup 1 31 lshift u< averts x-invalid-number 32 lshift
    else
      negate dup 1 31 lshift u<= averts x-invalid-number 32 lshift negate
    then ;

  \ Convert a 32.32 fixed point number into a signed integer to the right of the
  \ decimal point
  : f>s ( f32.32 -- n ) 32 arshift ;

  \ Get the floor of a 32.32 fixed point number
  : floor ( f32.32 -- f32.32 ) f>s s>f ;

  \ Get the portion of a 32.32 fixed point number right of the decimal point
  : fraction ( f32.32 -- f32.32 ) dup floor - ;

  \ Get the ceiling of a 32.32 fixed point number
  : ceiling ( f32.32 -- f32.32 )
    dup floor tuck - dup 0 > if
      drop 1 32 lshift +
    else
      0 < if 32 lshift negate + then
    then ;

  \ Multiply two 32.32 fixed point numbers
  : f* ( f1 f2 -- f3 ) 32 *rshift ;
  
  \ Divide two 32.32 fixed point numbers
  : f/ ( f1 f2 -- f3 ) [ 1 32 lshift ] literal swap */ ;
  
  \ Multiply two unsigned 32.32 fixed point numbers
  : fu* ( f1 f2 -- f3 ) 32 u*rshift ;
  
  \ Divide two unsigned 32.32 fixed point numbers
  : fu/ ( f1 f2 -- f3 ) 32 swap ulshift/ ;

  \ Get the decimal point index for a fixed point number in a string
  : parse-f32.32-point ( c-addr bytes - index matches )
    0 >r begin
      dup 0 u> if
	over c@ [char] ; = if true else 1 - swap 1 + swap r> 1 + >r false then
      else
	true
      then
    until
    0 = if r> 2drop 0 false else drop r> true then ;
  
  \ Parse a 32.32 fixed point number after the decimal point
  : parse-f32.32-after ( base c-addr bytes -- n matches )
    rot >r 0 begin
      over 0 > if
	rot rot 1 - 2dup + c@ r@ swap parse-digit if
	  32 lshift 3 roll r@ 32 lshift fu/ + false
	else
	  2drop 2drop r> drop 0 false true
	then
      else
	rot rot 2drop r> 32 lshift fu/ true true
      then
    until ;
  
  \ Main body of parsing a 32.32 fixed point number
  : parse-f32.32-main ( c-addr bytes base -- f32.32 matches )
    over 0 u> if
      >r 2dup parse-f32.32-point if
	r@ 3 pick 2 pick parse-digits if
	  dup 1 31 lshift u< if
	    32 lshift r> 4 roll 3 pick + 1 + 4 roll 4 roll - 1 -
	    dup 0 u> if
	      parse-f32.32-after if
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
  
  \ Parse a 32.32 fixed point number
  : parse-f32.32 ( c-addr bytes -- f32.32 matches )
    parse-base over 0 u> if
      2 pick c@ [char] - = if
	rot 1 + rot 1 - rot parse-f32.32-main dup if swap negate swap then
      else
	parse-f32.32-main
      then
    else
      2drop drop 0 false
    then ;
  
  forth-wordlist lambda-wordlist fixed-16.16-wordlist fixed-32.32-wordlist
  4 set-order

  \ Number parser
  : parse-compile-number-or-double-or-fixed ( c-addr bytes -- x matches )
    2dup 2>r parse-compile-number-or-double-or-fixed if
      2r> 2drop true
    else
      2r> parse-f32.32 if
	state @ if lit, then true
      else
	drop false
      then
    then ;

  \ Set number parser
  ' parse-compile-number-or-double-or-fixed 'handle-number !

[then]

base ! set-current set-order
