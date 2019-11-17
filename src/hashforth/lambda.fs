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

wordlist constant lambda-wordlist

forth-wordlist lambda-wordlist 2 set-order
lambda-wordlist set-current

: [: ( -- xt) ( in xt: -- )
  begin-atomic
  & branch align here 0 , 0 compile, latestxt :noname
  end-atomic ; immediate

: ;] ( -- ) ( in xt: -- )
  begin-atomic
  & exit 0 compile, here latestxt end>word
  rot here swap ! lit, latestxt-value !
  end-atomic ; immediate

: option ( ... flag true-xt -- ... )
  state @ if
    & swap & if & execute & else & drop & then
  else
    swap if execute else drop then
  then ; immediate

: choose ( ... flag true-xt false-xt -- ... )
  state @ if
    & rot & if & drop & execute & else & nip & execute & then
  else
    rot if drop execute else nip execute then
  then ; immediate

: loop-until ( ... xt -- ... )
  state @ if
    & >r & begin & r@ & execute & until & r> & drop
  else
    >r begin r@ execute until r> drop
  then ; immediate

: while-loop ( ... while-xt body-xt -- ... )
  state @ if
    & 2>r & begin & 2r@ & drop & execute & while & r@ & execute & repeat
    & 2r> & 2drop
  else
    2>r begin 2r@ drop execute while r@ execute repeat 2r> 2drop
  then ; immediate

: count-loop ( ... limit init xt -- ... )
  state @ if
    & rot & rot & ?do & i & swap & dup & >r & execute & r> & loop & drop
  else
    rot rot ?do i swap dup >r execute r> loop drop
  then ; immediate

: count+loop ( ... limit init xt -- ... )
  state @ if
    & rot & rot & ?do & i & swap & dup & >r & execute & r> & swap & +loop & drop
  else
    rot rot ?do i swap dup >r execute r> swap +loop drop
  then ; immediate

: fetch-advance ( a-addr1 count1 -- a-addr2 count2 x )
  & swap & dup & @ & rot & 1- & rot & cell+ & swap & rot ; immediate

: hide-3-below-2 ( x1 x2 x3 x4 x5 -- x4 x5 ) ( R: -- x3 x2 x1 )
  & rot & >r & rot & >r & rot & >r ; immediate

: show-3 ( -- x1 x2 x3 ) ( R: x3 x2 x1 -- ) & r> & r> & r> ; immediate

: iter ( a-addr count xt -- )
  [: over 0> ;] [: rot rot fetch-advance 3 pick hide-3-below-2 execute
     show-3 rot ;] while-loop
  2drop drop ;

: hide-4-below-3 ( x1 x2 x3 x4 x5 x6 x7 -- x5 x6 x7 ) ( R: -- x4 x3 x2 x1 )
  & (lit) 3 , & roll & >r & (lit) 3 , & roll & >r
  & (lit) 3 , & roll & >r & (lit) 3 , & roll & >r ; immediate

: show-4 ( -- x1 x2 x3 x4 ) ( R: x4 x3 x2 x1 -- )
  & r> & r> & r> & r> ; immediate

: iteri ( a-addr count xt -- )
  0
  [: 2 pick 0> ;]
  [: 3 roll 3 roll fetch-advance 3 pick swap 5 pick hide-4-below-3 execute
     show-4 3 roll 3 roll 1+ ;] while-loop
  2drop 2drop ;

: hide-4-below-2 ( x1 x2 x3 x4 x5 x6 -- x5 x6 ) ( R: -- x4 x3 x2 x1 )
  & rot & >r & rot & >r & rot & >r & rot & >r ; immediate

: show-4-below-1 ( x5 -- x1 x2 x3 x4 x5 ) ( R: x4 x3 x2 x1 -- )
  & r> & r> & r> & r> & (lit) 4 , & roll ; immediate

: (map) ( a-addr1 count1 a-addr2 xt -- count2 )
  [: 2 pick 0> ;]
  [: 3 roll 3 roll fetch-advance 3 pick hide-4-below-2 execute
     show-4-below-1 4 roll tuck ! cell+ 3 roll ;] while-loop
  2drop 2drop ;

: map ( a-addr1 count1 a-addr2 xt -- a-addr2 count2 )
  rot rot 2>r 2r@ rot (map) 2r> swap ;

: hide-6-below-2 ( x1 x2 x3 x4 x5 x6 x7 x8 -- x7 x8 )
  ( R: -- x6 x5 x4 x3 x2 x1 )
  & rot & >r & rot & >r & rot & >r & rot & >r & rot & >r & rot & >r ; immediate

: show-6-below-1 ( x7 -- x1 x2 x3 x4 x5 x6 x7 ) ( R: x6 x5 x4 x3 x2 x1 -- )
  & r> & r> & r> & r> & r> & r> & (lit) 6 , & roll ; immediate

: (filter) ( a-addr1 count1 a-addr2 xt -- count2 )
  0
  [: 3 pick 0> ;]
  [: 4 roll 4 roll fetch-advance dup 5 pick hide-6-below-2 execute
     show-6-below-1
     [: ( a2 xt c2 a1 c1 x ) 5 roll tuck ! cell+ 3 roll 1+ 4 roll swap ;]
     [: drop 4 roll 4 roll 4 roll ;] choose ;] while-loop
  rot rot 2drop rot rot 2drop ;

: filter ( a-addr1 count1 a-addr2 xt -- a-addr2 count2 )
  swap >r r@ swap (filter) r> swap ;

: hide-7-below-2 ( x1 x2 x3 x4 x5 x6 x7 x8 x9 -- x8 x9 )
  ( R: -- x7 x6 x5 x4 x3 x2 x1 )
  & rot & >r & rot & >r & rot & >r & rot & >r & rot & >r & rot & >r
  & rot & >r ; immediate

: show-7-below-1 ( x8 -- x1 x2 x3 x4 x5 x6 x7 x8 )
  ( R: x7 x6 x5 x4 x3 x2 x1 -- )
  & r> & r> & r> & r> & r> & r> & r> & (lit) 7 , & roll ; immediate

: show-6-below-1 ( x7 -- x1 x2 x3 x4 x5 x6 x7 ) ( R: x6 x5 x4 x3 x2 x1 -- )
  & r> & r> & r> & r> & r> & r> & (lit) 6 , & roll ; immediate

: (filter-map) ( a-addr1 count1 a-addr2 xt-filter xt-map -- count2 )
  0
  [: 4 pick 0> ;]
  [: 5 roll 5 roll fetch-advance dup 6 pick hide-7-below-2 execute
     show-7-below-1
     [: 4 pick hide-6-below-2 execute show-6-below-1 6 roll tuck !
        cell+ 3 roll 1+ 5 roll 5 roll rot ;]
     [: drop 5 roll 5 roll 5 roll 5 roll ;] choose ;] while-loop
  rot rot 2drop rot rot 2drop nip ;

: filter-map ( a-addr1 count1 a-addr2 xt-filter xt-map -- a-addr2 count2 )
  rot >r r@ rot rot (filter-map) r> swap ;

base ! set-current set-order
