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

cell 2 > [if]

  wordlist constant fixed-16.16-wordlist
  forth-wordlist lambda-wordlist fixed-16.16-wordlist 3 set-order
  fixed-16.16-wordlist set-current

  \ Invalid number exception
  : x-invalid-number ( -- ) space ." invalid number" cr ;
  
  \ Convert a signed integer into a 16.16 fixed point number
  : s>f ( n -- f16.16 )
    dup 0 > if
      dup 32767 u<= averts x-invalid-number 16 lshift
    else
      negate dup 32768 u<= averts x-invalid-number 16 lshift negate
    then ;

  \ Convert a 16.16 fixed point number into a signed integer to the right of the
  \ decimal point, rounding to zero
  : f>s ( f16.16 -- n ) [ 1 16 lshift ] literal / ;

  \ Convert a 16.16 fixed point number into a signed integer to the right of the
  \ decimal point, rounding down
  : f>s-down ( f16.16 -- n ) 16 arshift ;

  \ Get the floor of a 16.16 fixed point number
  : floor ( f16.16 -- f16.16 ) f>s-down s>f ;

  \ Get the portion of a 16.16 fixed point number right of the decimal point
  : fraction ( f16.16 -- f16.16 ) dup floor - ;

  \ Get the ceiling of a 16.16 fixed point number
  : ceiling ( f16.16 -- f16.16 )
    dup f>s-down s>f tuck - 0 > if [ 1 16 lshift ] literal + then ;

  \ Multiply two 16.16 fixed point numbers
  : f* ( f1 f2 -- f3 )
    [ cell 8 < ] [if]
      16 *rshift
    [else]
      * 16 arshift
    [then] ;
  
  \ Divide two 16.16 fixed point numbers
  : f/ ( f1 f2 -- f3 )
    [ cell 8 < ] [if]
      [ 1 16 lshift ] literal swap */
    [else]
      >r [ 1 16 lshift ] literal * r> /
    [then] ;

  \ Multiply two unsigned 16.16 fixed point numbers
  : fu* ( f1 f2 -- f3 )
    [ cell 8 < ] [if]
      16 u*rshift
    [else]
      * 16 rshift
    [then] ;
  
  \ Divide two unsigned 16.16 fixed point numbers
  : fu/ ( f1 f2 -- f3 )
    [ cell 8 < ] [if]
      16 swap ulshift/
    [else]
      >r 16 lshift r> u/
    [then] ;

  \ Get the decimal point index for a fixed point number in a string
  : parse-f16.16-point ( c-addr bytes - index matches )
    0 >r begin
      dup 0 u> if
	over c@ [char] , = if true else 1 - swap 1 + swap r> 1 + >r false then
      else
	true
      then
    until
    0 = if r> 2drop 0 false else drop r> true then ;
  
  \ Parse a 16.16 fixed point number after the decimal point
  : parse-f16.16-after ( base c-addr bytes -- n matches )
    rot >r 0 begin
      over 0 > if
	rot rot 1 - 2dup + c@ r@ swap parse-digit if
	  16 lshift 3 roll r@ 16 lshift fu/ + false
	else
	  2drop 2drop r> drop 0 false true
	then
      else
	rot rot 2drop r> 16 lshift fu/ true true
      then
    until ;
  
  \ Main body of parsing a 16.16 fixed point number
  : parse-f16.16-main ( c-addr bytes base -- f16.16 matches )
    over 0 u> if
      >r 2dup parse-f16.16-point if
	r@ 3 pick 2 pick parse-digits if
	  dup 32768 u< if
	    16 lshift r> 4 roll 3 pick + 1 + 4 roll 4 roll - 1 -
	    dup 0 u> if
	      parse-f16.16-after if
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
  
  \ Parse a 16.16 fixed point number
  : parse-f16.16 ( c-addr bytes -- f16.16 matches )
    parse-base over 0 u> if
      2 pick c@ [char] - = if
	rot 1 + rot 1 - rot parse-f16.16-main dup if swap negate swap then
      else
	parse-f16.16-main
      then
    else
      2drop drop 0 false
    then ;

  forth-wordlist lambda-wordlist fixed-wordlist fixed-16.16-wordlist 4 set-order
  
  \ Number parser
  : parse-compile-number-or-double-or-fixed ( c-addr bytes -- x matches )
    2dup 2>r parse-compile-number-or-double-or-fixed if
      2r> 2drop true
    else
      2r> parse-f16.16 if
	state @ if lit, then true
      else
	drop false
      then
    then ;

  \ Set number parser
  ' parse-compile-number-or-double-or-fixed 'handle-number !

[then]

base ! set-current set-order
