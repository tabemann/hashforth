\ Copyright (c) 2018-2019, Travis Bemann
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

require buffer.fs

wordlist constant hashforth-wordlist
wordlist constant hashforth-asm-wordlist

forth-wordlist hashforth-wordlist 2 set-order
hashforth-wordlist set-current

variable header-buffer
variable code-buffer
variable storage-buffer

0 constant cell-16
1 constant cell-32
2 constant cell-64

0 constant token-8-16
1 constant token-16
2 constant token-16-32
3 constant token-32

variable target-cell cell-64 target-cell !
variable target-token token-32 target-token !
variable mem-size 0 mem-size !
variable max-word-count 0 max-word-count !
variable max-return-stack-count 0 max-return-stack-count !

: header-8, ( value -- )
  $ff and header-buffer @ append-byte-buffer ;

: header-16, ( value -- )
  $ffff and here h! here halfword-size header-buffer @ append-buffer ;

: header-32, ( value -- )
  $ffffffff and here w! here word-size header-buffer @ append-buffer ;

: header-64, ( value -- )
  here ! here cell-size header-buffer @ append-buffer ;

: header, ( value -- )
  target-cell @ case
    cell-16 of header-16, endof
    cell-32 of header-32, endof
    cell-64 of header-64, endof
  endcase ;

: header-array, ( c-addr bytes -- ) header-buffer @ append-buffer ;

: set-data ( c-addr bytes -- ) code-buffer @ append-buffer ;

: set-fill-data ( bytes c -- )
  over allocate! dup 3 pick 3 roll fill tuck swap set-data free! ;

: set-cell-data ( x -- ) here ! here cell-size set-data ;

variable name-table-offset
variable info-table-offset

begin-structure name-table-entry-size
  field: name-offset
  field: name-length
end-structure

begin-structure info-table-entry-size
  field: info-flags
  field: info-next
end-structure

: init-asm
  ( cell-type token-type mem-size max-word-count max-return-stack-count -- )
  header-buffer @ if
    header-buffer @ clear-buffer
  else
    65536 new-buffer header-buffer !
  then
  code-buffer @ if
    code-buffer @ clear-buffer
  else
    1024 1024 * new-buffer code-buffer !
  then
  storage-buffer @ if
    storage-buffer @ clear-buffer
  else
    65536 new-buffer storage-buffer !
  then
  max-return-stack-count ! max-word-count ! mem-size ! target-token !
  target-cell !
  target-cell @ header-8, target-token @ header-8, mem-size @ header,
  max-word-count @ header, max-return-stack-count @ header,
  max-word-count @ name-table-entry-size * 0 set-fill-data
  max-word-count @ info-table-entry-size * 0 set-fill-data
  0 name-table-offset !
  max-word-count @ name-table-entry-size * info-table-offset ! ;

cell-64 token-16-32 1024 1024 * 32 * 16384 1024 init-asm

: target-cell-size ( -- bytes )
  target-cell @ case
    cell-16 of 2 endof
    cell-32 of 4 endof
    cell-64 of 8 endof
  endcase ;

: target-half-token-size ( -- bytes )
  target-token @ case
    token-8-16 of 1 endof
    token-16 of 2 endof
    token-16-32 of 2 endof
    token-32 of 4 endof
  endcase ;

: target-full-token-size ( -- bytes )
  target-token @ case
    token-8-16 of 2 endof
    token-16 of 2 endof
    token-16-32 of 4 endof
    token-32 of 4 endof
  endcase ;

: token-8-16, ( token -- )
  dup $7f > if
    dup $ff and $80 or code-buffer @ append-byte-buffer
    7 rshift 1- $ff and code-buffer @ append-byte-buffer
  else
    code-buffer @ append-byte-buffer
  then ;

: token-16, ( token -- )
  $ffff and here h! here halfword-size code-buffer @ append-buffer ;

: token-16-32, ( token -- )
  dup $7fff > if
    dup $ffff and $8000 or here h! here halfword-size
    code-buffer @ append-buffer
    15 rshift 1- $ffff and here h! here halfword-size
    code-buffer @ append-buffer
  else
    here h! here halfword-size code-buffer @ append-buffer
  then ;

: token-32, ( token -- )
  $ffffffff and here w! here word-size code-buffer @ append-buffer ;

: token, ( token -- )
\  cr ." compiling token: " dup .
  target-token @ case
    token-8-16 of token-8-16, endof
    token-16 of token-16, endof
    token-16-32 of token-16-32, endof
    token-32 of token-32, endof
  endcase ;

: arg-16, ( value -- )
  $ffff and here h! here halfword-size code-buffer @ append-buffer ;

: arg-32, ( value -- )
  $ffffffff and here w! here word-size code-buffer @ append-buffer ;

: arg-64, ( value -- )
  here ! here cell-size code-buffer @ append-buffer ;

: arg, ( value -- )
  target-cell @ case
    cell-16 of arg-16, endof
    cell-32 of arg-32, endof
    cell-64 of arg-64, endof
  endcase ;

: set-arg-16 ( value offset -- )
  swap $ffff and here h! here halfword-size rot code-buffer @ write-buffer ;

: set-arg-32 ( value offset -- )
  swap $ffffffff and here w! here word-size rot code-buffer @ write-buffer ;

: set-arg-64 ( value offset -- )
  swap here ! here cell-size rot code-buffer @ write-buffer ;

: set-arg ( value offset -- )
  target-cell @ case
    cell-16 of set-arg-16 endof
    cell-32 of set-arg-32 endof
    cell-64 of set-arg-64 endof
  endcase ;

variable current-token 59 current-token !

: next-token ( -- token ) current-token @ dup 1+ current-token ! ;

0 constant headers-end
1 constant colon-word
2 constant create-word

: get-ref ( -- ref ) code-buffer @ get-buffer-length ;

: set-name-info ( addr bytes token flags -- )
\  3 pick 3 pick type ." : " over (.) cr
  get-ref here name-offset ! 2 pick here name-length !
  here name-table-entry-size
  3 pick name-table-entry-size * name-table-offset @ +
  code-buffer @ write-buffer
  here info-flags ! dup dup 0> if 1- then here info-next !
  here info-table-entry-size
  rot info-table-entry-size * info-table-offset @ +
  code-buffer @ write-buffer
  set-data ;

: make-colon-word ( token name-addr name-length flags -- )
  colon-word header-8, 3 pick header, over get-ref + header, 3 roll swap
  set-name-info ;

: make-create-word ( token name-addr name-length flags -- )
  create-word header-8, 3 pick header, over get-ref + header, 3 roll swap
  set-name-info ;

: make-create-word-with-offset ( token name-addr name-length flags offset -- )
  create-word header-8, 4 pick header, header, 3 roll swap
  set-name-info ;

: end-headers ( -- )
  headers-end header-8, code-buffer @ get-buffer-length header, ;

: write-asm-to-file ( name-addr name-length -- )
  end-headers w/o create-file abort" unable to open file"
  header-buffer @ get-buffer 2 pick write-file abort" unable to write file"
  code-buffer @ get-buffer 2 pick write-file abort" unable to write file"
  storage-buffer @ get-buffer 2 pick write-file abort" unable to write file"
  close-file ;

1024 constant source-space-size
source-space-size buffer: source-space

: add-source-to-storage ( name-addr name-length -- )
  r/o open-file abort" unable to open file"
  begin
    source-space source-space-size 2 pick read-file abort" unable to read file"
    source-space swap tuck storage-buffer @ append-buffer
  source-space-size < until
  close-file
  storage-buffer @ get-buffer-length 0 > if
    storage-buffer @ get-buffer 1- + c@ newline <> if
      newline storage-buffer @ append-byte-buffer
    then
  then ;

0 constant no-flag
1 constant immediate-flag
2 constant compile-only-flag
4 constant hidden-flag

: 3dup ( x1 x2 x3 -- x1 x2 x3 x1 x2 x3 ) 2 pick 2 pick 2 pick ;

\ : CREATE-COMPILE ( c-addr bytes token -- )
\   OVER 1+ NEW-BUFFER [CHAR] . OVER APPEND-BYTE-BUFFER 3 ROLL 3 ROLL 2 PICK
\   APPEND-BUFFER DUP GET-BUFFER CREATE-WITH-NAME SWAP , DESTROY-BUFFER
\   DOES> @ TOKEN, ;

: create-compile ( c-addr bytes token -- )
  rot rot create-with-name , does> @ token, ;

: create-token-lit ( c-addr bytes token -- )
  over 1+ new-buffer [char] & over append-byte-buffer 3 roll 3 roll 2 pick
  append-buffer dup get-buffer create-with-name swap , destroy-buffer
  does> 5 token, @ arg, ;

: create-token ( c-addr bytes token -- )
  over 1+ new-buffer [char] ^ over append-byte-buffer 3 roll 3 roll 2 pick
  append-buffer dup get-buffer create-with-name swap , destroy-buffer
  does> @ ;

: create-words ( c-addr bytes token )
  3dup create-compile 3dup create-token-lit create-token ;

: create-token-and-lit-token ( c-addr bytes token )
  3dup create-token-lit create-token ;

: primitive ( "name" token -- )
  parse-name rot 3dup create-words no-flag set-name-info ;

: primitive-immediate ( "name" token -- )
  parse-name rot 3dup create-words immediate-flag set-name-info ;

: primitive-compile-only ( "name" token -- )
  parse-name rot 3dup create-words compile-only-flag set-name-info ;

: primitive-immediate-compile-only ( "name" token -- )
  parse-name rot 3dup create-words immediate-flag compile-only-flag or
  set-name-info ;

: vm forth-wordlist hashforth-wordlist hashforth-asm-wordlist
  3 set-order ; immediate

: not-vm hashforth-asm-wordlist hashforth-wordlist forth-wordlist
  3 set-order ; immediate

not-vm hashforth-asm-wordlist set-current

: define-word ( "name" -- )
  parse-name next-token 3dup create-words rot rot
  no-flag make-colon-word ;

: define-word-immediate ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  immediate-flag make-colon-word ;

: define-word-compile-only ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  compile-only-flag make-colon-word ;

: define-word-immediate-compile-only ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  immediate-flag compile-only-flag or make-colon-word ;

: define-word-created ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  no-flag make-create-word ;

: define-word-created-immediate ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  immediate-flag make-create-word ;

: define-word-created-compile-only ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  compile-only-flag make-create-word ;

: define-word-created-immediate-compile-only ( "name" -- )
  parse-name next-token 3dup create-words rot rot 
  immediate-flag compile-only-flag or make-create-word ;

: define-word-created-with-offset ( "name" offset -- )
  parse-name next-token 3dup create-words rot rot 
  no-flag 4 roll make-create-word-with-offset ;

: non-define-word ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  no-flag make-colon-word ;

: non-define-word-immediate ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  immediate-flag make-colon-word ;

: non-define-word-compile-only ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  compile-only-flag make-colon-word ;

: non-define-word-immediate-compile-only ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  immediate-flag compile-only-flag or make-colon-word ;

: non-define-word-created ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  no-flag make-create-word ;

: non-define-word-created-immediate ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  immediate-flag make-create-word ;

: non-define-word-created-compile-only ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  compile-only-flag make-create-word ;

: non-define-word-created-immediate-compile-only ( "name" -- )
  parse-name next-token 3dup create-token-and-lit-token rot rot 
  immediate-flag compile-only-flag or make-create-word ;

0 primitive end
1 primitive nop
2 primitive exit
3 primitive branch
4 primitive 0branch
5 primitive (lit)
6 primitive (data)
7 primitive new-colon
8 primitive new-create
9 primitive set-does>
10 primitive finish
11 primitive execute
12 primitive drop
13 primitive dup
14 primitive swap
15 primitive over
16 primitive rot
17 primitive pick
18 primitive roll
19 primitive @
20 primitive !
21 primitive c@
22 primitive c!
23 primitive =
24 primitive <>
25 primitive <
26 primitive >
27 primitive u<
28 primitive u>
29 primitive not
30 primitive and
31 primitive or
32 primitive xor
33 primitive lshift
34 primitive rshift
35 primitive arshift
36 primitive +
37 primitive -
38 primitive *
39 primitive /
40 primitive mod
41 primitive u/
42 primitive umod
43 primitive mux
44 primitive /mux
45 primitive r@
46 primitive >r
47 primitive r>
48 primitive sp@
49 primitive sp!
50 primitive rp@
51 primitive rp!
52 primitive >body
53 primitive h@
54 primitive h!
55 primitive w@
56 primitive w!
57 primitive set-word-count
58 primitive sys

name-table-offset @ define-word-created-with-offset name-table
info-table-offset @ define-word-created-with-offset info-table

: end-word ( -- ) vm exit end not-vm ;

: lit ( x -- ) vm (lit) not-vm arg, ;

: back-ref ( "name" -- ) create get-ref , does> @ ;

: +branch-fore ( "name" -- )
  create vm branch not-vm get-ref , 0 arg, does> @ get-ref swap set-arg ;

: +0branch-fore ( "name" -- )
  create vm 0branch not-vm get-ref , 0 arg, does> @ get-ref swap set-arg ;

: +branch-back ( x -- ) vm branch not-vm arg, ;

: +0branch-back ( x -- ) vm 0branch not-vm arg, ;

: +if ( -- forward-ref ) vm 0branch not-vm get-ref 0 arg, ;

: +else ( forward-ref -- forward-ref )
  vm branch not-vm get-ref 0 arg, swap get-ref swap set-arg ;

: +then ( forward-ref -- ) get-ref swap set-arg ;

: +begin ( -- backward-ref ) get-ref ;

: +again ( backward-ref -- ) +branch-back ;

: +until ( backward-ref -- ) +0branch-back ;

: +while ( -- forward-ref ) vm 0branch not-vm get-ref 0 arg, ;

: +repeat ( backward-ref forward-ref -- )
  swap +branch-back get-ref swap set-arg ;

: +data ( c-addr bytes -- )
  vm (data) not-vm dup arg, tuck set-data vm lit not-vm ;

base ! set-current set-order
