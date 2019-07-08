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

\ Add a value to an address
: +! ( n addr ) dup @ rot + swap ! ;

: bin 2 base ! ;
: binary 2 base ! ;

: oct 8 base ! ;
: octal 8 base ! ;

: dec 10 base ! ;
: decimal 10 base ! ;

: hex 16 base ! ;

\ Execute an xt with a specified BASE set, restoring BASE afterwards even if
\ an exception occurs
: base-execute ( i*x xt base -- j*x ) base @ >r base ! try r> base ! ?reraise ;

\ Execute . with a specified base
: base. ( n base -- ) ['] . swap base-execute ;

\ Execute (.) with a specified base
: (base.) ( n base -- ) ['] (.) swap base-execute ;

\ Execute U. with a specified base
: ubase. ( u base -- ) ['] u. swap base-execute ;

\ Exxecute (U.) with a specified base
: (ubase.) ( u base -- ) ['] (u.) swap base-execute ;

: 1+ 1 + ;

: 1- 1 - ;

: 0= 0 = ;

: 0<> 0 <> ;

: 0< 0 < ;

: 0> 0 > ;

: 0<= 0 <= ;

: 0>= 0 >= ;

: abs dup 0< if negate then ;

: postpone ' dup immediate? if
    compile,
  else
    compile (lit) , compile compile,
  then ; immediate compile-only

: & postpone postpone ; immediate

: 2swap 3 roll 3 roll ;

: 2over 3 pick 3 pick ;

: 2rot 5 roll 5 roll ;

: 2! tuck ! cell+ ! ;

: 2@ dup @ swap cell+ @ swap ;

: 2, here 2! [ 2 cells ] literal allot ;

: 2>r r> rot rot swap >r >r >r ;

: 2r> r> r> r> swap rot >r ;

: 2r@ r> r> r> 2dup swap rot >r rot >r rot >r ;

: 2variable create 0 0 2, ;

: 2constant create 2, does> 2@ ;

: defer create ['] abort , does> @ execute ;

: defer! >body ! ;

: defer@ >body @ ;

: action-of state @ if & ['] & defer@ else ' defer@ then ; immediate

: is state @ if & ['] & defer! else ' defer! then ; immediate

: do & (lit) here dup 0 , & >r & 2>r here ; immediate compile-only

: ?do & (lit) here 0 , & >r & 2dup & 2>r & <> & 0branch here 0 , here
  ; immediate compile-only

: loop & 2r> & 1+ & 2dup & 2>r & = & 0branch , here swap ! here swap !
  & 2r> & 2drop & r> & drop ; immediate compile-only

: +loop & 2r> & rot & dup & 0>= & if
    & swap & dup & (lit) 3 , & pick & < & rot & rot & +
    & dup & (lit) 3 , & pick & >= & rot & and & rot & rot & 2>r
  & else
    & swap & dup & (lit) 3 , & pick & >= & rot & rot & +
    & dup & (lit) 3 , & pick & < & rot & and & rot & rot & 2>r
  & then
  & 0branch , here swap ! here swap ! & 2r> & 2drop & r> & drop ;
  immediate compile-only

: leave r> 2r> 2drop >r ;

: unloop r> 2r> 2drop r> drop >r ;

: i r> r@ swap >r ;

: j 2r> 2r> r@ 4 roll 4 roll 4 roll 4 roll 2>r 2>r ;

: case 0 ; immediate compile-only

: of & over & = postpone if & drop ; immediate compile-only

: endof swap ?dup if here swap ! then
  & branch here 0 , swap postpone then ; immediate compile-only

: endcase & drop ?dup if here swap ! then ; immediate compile-only

: ofstr
  & (lit) 3 , & pick & (lit) 3 , & pick & equal-strings? postpone if & 2drop
; immediate compile-only

: ofstrcase
  & (lit) 3 , & pick & (lit) 3 , & pick & equal-name? postpone if & 2drop
; immediate compile-only

: endcasestr & 2drop ?dup if here swap ! then ; immediate compile-only

: fill ( addr count char -- )
  swap begin dup 0 > while rot 2 pick over c! 1 + rot rot 1 - repeat
  drop 2drop ;

: align here cell-size mod dup 0<> if
    cell-size swap - allot
  else
    drop
  then ;

: aligned dup cell-size mod dup 0<> if
    cell-size swap - +
  else
    drop
  then ;

: walign here 4 mod dup 0<> if
    4 swap - allot
  else
    drop
  then ;

: waligned dup 4 mod dup 0<> if
    4 swap - +
  else
    drop
  then ;

: halign here 2 mod dup 0<> if
    2 swap - allot
  else
    drop
  then ;

: haligned dup 2 mod dup 0<> if
    2 swap - +
  else
    drop
  then ;

: begin-structure create here 0 0 , does> @ ;

: end-structure swap ! ;

: +field create over , + does> @ + ;

: cfield: create dup , 1 + does> @ + ;

: hfield: create haligned dup , 2 + does> @ + ;

: wfield: create waligned dup , 4 + does> @ + ;

: field: create aligned dup , [ cell-size ] literal + does> @ + ;

\ Add two times
: time+ ( s1 ns1 s2 ns2 -- s3 ns3 )
  rot + dup 1000000000 >= if
    1000000000 - rot rot + 1 + swap
  else dup 0 < if
    1000000000 + rot rot + 1 - swap
  else
    rot rot + swap
  then then ;

\ Get a sign
: sign ( n -- ) dup 0 > if 1 else 0 < if -1 else 0 then then ;

\ Turn a counted string into a normal string
: count ( c-addr1 - c-addr2 u ) dup c@ swap 1 + swap ;

\ Drop a given number of cells in addition to the count
: drops ( x*u1 u1 -- ) cells sp@ + cell+ sp! ;

\ Drop a given number of cells in addition to the count except for the cell
\ directly beneath the count
: nips ( x*u1 x1 u1 -- x1 ) swap >r 1- drops r> ;

\ s" constant
2 constant s"-length
create s"-data char s c, char " c,
s"-data s"-length 2constant s"-constant

\ ." constant
2 constant ."-length
create ."-data char c c, char " c,
."-data ."-length 2constant ."-constant

\ c" constant
2 constant c"-length
create c"-data char c c, char " c,
c"-data c"-length 2constant c"-constant

\ Implement the [else] in [if]/[else]/[then] for conditional
\ execution/compilation
: [else] ( -- )
  1 begin
    begin parse-name dup while
      case
	s" [if]" ofstrcase 1 + endof
        s" [else]" ofstrcase 1 - dup if 1 + then endof
        s" [then]" ofstrcase 1 - endof
	s" \" ofstrcase
	  begin
	    input-left? if input-newline? advance->in else true then
	  until
	endof
	s" (" ofstrcase
	  begin
	    input-left? if input-close-paren? advance->in else true then
	  until
	endof
	s"-constant ofstrcase parse-string 2drop endof
	c"-constant ofstrcase parse-string 2drop endof
	."-constant ofstrcase parse-string 2drop endof
	s" .(" ofstrcase parse-paren-string 2drop endof
	s" char" ofstrcase state @ not if parse-name 2drop then endof
	s" [char]" ofstrcase parse-name 2drop endof
	s" '" ofstrcase state @ not if parse-name 2drop then endof
	s" [']" ofstrcase parse-name 2drop endof
	s" postpone" ofstrcase parse-name 2drop endof
	s" &" ofstrcase parse-name 2drop endof
      endcasestr
      ?dup 0 = if exit then
    repeat 2drop
  refill input# @ 0 = until
  drop
; immediate

\ Start conditional execution/compilation
: [if] ( flag -- ) 0 = if & [else] then ; immediate

\ Finish conditional execution/compilation
: [then] ( -- ) ; immediate

\ Decompile 8/16-bit token
: decompile-token-8-16 ( addr -- addr token )
  dup c@ dup $80 u< if
    swap 1 + swap
  else
    $7f and swap 1 + dup c@ 7 lshift rot or swap 1 + swap
  then ;

\ Decompile 16-bit token
: decompile-token-16 ( addr -- addr token ) dup h@ swap 2 + swap ;

\ Decompile 16/32-bit token
: decompile-token-16-32 ( addr -- addr token )
  dup h@ dup $8000 u< if
    swap 2 + swap
  else
    $7fff and swap 2 + dup h@ 15 lshift rot or swap 2 + swap
  then ;

\ Decompile 32-bit token
: decompile-token-32 ( addr -- addr token ) dup w@ swap 4 + swap ;

\ Decompile token
: decompile-token ( addr -- addr token )
  half-token-size 1 = if
    decompile-token-8-16
  else
    half-token-size 2 = full-token-size 2 = and if
      decompile-token-16
    else
      half-token-size 2 = full-token-size 4 = and if
	decompile-token-16-32
      else
	decompile-token-32
      then
    then
  then ;

\ Decompile a branch token
: decompile-branch ( addr -- addr )
  ." branch $" dup @ dup ['] . 16 base-execute
  over cell+ decompile-token nip 0 = if nip else drop cell+ then ;

\ Decompile a 0branch token
: decompile-0branch ( addr -- addr )
  ." 0branch $" dup @ ['] . 16 base-execute cell+ ;

\ Decompile a (lit) token
: decompile-(lit) ( addr -- addr ) ." (lit) " dup @ . cell+ ;

\ Decompile a (litc) token
: decompile-(litc) ( addr -- addr ) ." (litc) " dup c@ . 1 + ;

\ Decompile a (lith) token
: decompile-(lith) ( addr -- addr ) ." (lith) " dup h@ . 2 + ;

\ Decompile a (litw) token
: decompile-(litw) ( addr -- addr ) ." (litw) " dup w@ . 4 + ;

\ Print out a string with control characters and characters over $7F replaced
: type-replaced-string ( addr bytes -- )
  begin dup 0 > while
    swap dup c@ dup $21 < over $7f > or if drop [char] . then emit
    1 + swap 1 -
  repeat
  2drop ;

\ Print out a string in hex
: type-hex-string ( addr bytes -- )
  begin dup 0 > while
    swap dup c@
    dup $f0 and 4 rshift dup 9 > if 10 - [char] A + else [char] 0 + then emit
    $0f and dup 9 > if 10 - [char] A + else [char] 0 + then emit space
    1 + swap 1 -
  repeat
  2drop ;

\ Decompile a (data) token
: decompile-(data) ( addr -- addr )
  ." (data) size: " dup @ swap cell+ swap dup . 2dup type-replaced-string
  space 2dup type-hex-string + ;

\ Decompile an operation
: decompile-op ( addr token -- addr )
  case
    ['] branch of decompile-branch endof
    ['] 0branch of decompile-0branch endof
    ['] (lit) of decompile-(lit) endof
    ['] (litc) of decompile-(litc) endof
    ['] (lith) of decompile-(lith) endof
    ['] (litw) of decompile-(litw) endof
    ['] (data) of decompile-(data) endof
    dup word>name type
  endcase ;

\ Decompile at a starting address
: decompile-at-address ( addr -- )
  begin
    dup decompile-token dup 0 <> if
      rot cr ." $" ['] . 16 base-execute decompile-op false
    else
      drop 2drop true
    then
  until ;

\ Decompile a specified word
: decompile ( xt -- ) word>start decompile-at-address ;

\ Decompile a specified word
: see ( "name" -- ) ' decompile ;

 \ Search for the word an address is in
: find-word-by-address ( addr -- xt found )
  latestxt begin
    dup 0 > if
      dup word>start 0 <> if
	dup word>start 2 pick <= over word>end 3 pick > and if
	  nip true true
	else
	  1 - false
	then
      else
	1 - false
      then
    else
      2drop 0 false true
    then
  until ;

\ Decompile a word containing an address
: decompile-by-address ( addr -- )
  find-word-by-address if decompile else drop cr ." ???" then ;

\ Actually display a backtrace
: do-backtrace ( -- )
  rp@ begin dup rbase @ < while
    dup @ dup find-word-by-address if
      cr swap ." $" 16 base. ." ("
      word>name over 0 <> if type else 2drop ." <anonymous>" then ." ) "
    else
      drop cr ." $" 16 base. ." (???) "
    then
    cell+
  repeat
  drop ;

\ Display a backtrace
: backtrace ( -- )
  single-task-io @ true single-task-io ! do-backtrace single-task-io ! ;

' backtrace 'global-handler !
' backtrace 'global-bye-handler !

\ Unmask interrupt
: unmask-int ( int -- ) 1 swap lshift not 0 adjust-int-mask ;

\ Segfault interrupt
0 constant segv-int

\ Invalid token interrupt
1 constant token-int

\ Divide by zero interrupt
2 constant divzero-int

\ Illegal instruction interrupt
3 constant illegal-int

\ Bus error interrupt
4 constant bus-int

\ Realtime alarm interrupt
5 constant alarm-real-int

\ User-mode alarm interrupt
6 constant alarm-virtual-int

\ User/system alarm interrupt
7 constant alarm-prof-int

\ Realtime alarm type
0 constant alarm-real

\ User-mode alarm type
1 constant alarm-virtual

\ User/system alarm type
2 constant alarm-prof

\ Segfault exception
: x-segv ( -- ) space ." segmentation fault" cr ;

\ Invalid token exception
: x-token ( -- ) space ." invalid token" cr ;

\ Divide by zero exception
: x-divzero ( -- ) space ." divide by zero" cr ;

\ Illegal instruction exception
: x-illegal ( -- ) space ." illegal instruction" cr ;

\ Bus error exception
: x-bus ( -- ) space ." bus error" cr ;

\ Segfault handler
: handle-segv ( -- )
  rp@ handler @ < rp@ rbase @ < and if
    segv-int unmask-int ['] x-segv ?raise
  else
    segv-int unmask-int backtrace x-segv
    ['] outer try if s" error " output-fd @ write 2drop then bye
  then ;

\ Invalid token handler
: handle-token ( -- )
  rp@ handler @ < rp@ rbase @ < and if
    token-int unmask-int ['] x-token ?raise
  else
    token-int unmask-int backtrace x-token
    ['] outer try if s" error " output-fd @ write 2drop then bye
  then ;

\ Divide by zero handler
: handle-divzero ( -- )
  rp@ handler @ < rp@ rbase @ < and if
    divzero-int unmask-int ['] x-divzero ?raise
  else
    divzero-int unmask-int backtrace x-divzero
    ['] outer try if s" error " output-fd @ write 2drop then bye
  then ;

\ Illegal instruction handler
: handle-illegal ( -- )
    rp@ handler @ < rp@ rbase @ < and if
    illegal-int unmask-int ['] x-illegal ?raise
  else
    illegal-int unmask-int backtrace x-illegal
    ['] outer try if s" error " output-fd @ write 2drop then bye
  then ;

\ Bus error handler
: handle-bus ( -- )
  rp@ handler @ < rp@ rbase @ < and if
    bus-int unmask-int ['] x-bus ?raise
  else
    bus-int unmask-int space backtrace x-bus
    ['] outer try if s" error " output-fd @ write 2drop then bye
  then ;

\ Set interrupt handlers
' handle-segv segv-int set-int-handler
' handle-token token-int set-int-handler
' handle-divzero divzero-int set-int-handler
' handle-illegal illegal-int set-int-handler
' handle-bus bus-int set-int-handler
