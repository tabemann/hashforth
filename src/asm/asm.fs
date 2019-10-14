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

\ REQUIRE buffer.fs

wordlist constant hashforth-wordlist
wordlist constant hashforth-asm-wordlist

forth-wordlist buffer-wordlist hashforth-wordlist 3 set-order
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

: allocate-buffer new-buffer ;

: header-8, ( value -- )
  $ff and header-buffer @ append-byte-buffer ;

: header-16, ( value -- )
  $ffff and here h! here 2 header-buffer @ append-buffer ;

: header-32, ( value -- )
  $ffffffff and here w! here 4 header-buffer @ append-buffer ;

: header-64, ( value -- )
  here ! here cell header-buffer @ append-buffer ;

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

: set-cell-data ( x -- ) here ! here cell set-data ;

variable name-table-offset
variable info-table-offset

begin-structure name-table-entry-size
  field: name-offset
  field: name-length
end-structure

begin-structure info-table-entry-size
  field: info-flags
  field: info-next
  field: info-start
  field: info-end
end-structure

: init-asm
  ( cell-type token-type mem-size max-word-count max-return-stack-count -- )
  header-buffer @ if
    header-buffer @ clear-buffer
  else
    65536 allocate-buffer header-buffer !
  then
  code-buffer @ if
    code-buffer @ clear-buffer
  else
    1024 1024 * allocate-buffer code-buffer !
  then
  storage-buffer @ if
    storage-buffer @ clear-buffer
  else
    120988 2 * allocate-buffer storage-buffer !
  then
  max-return-stack-count ! max-word-count ! mem-size ! target-token !
  target-cell !
  target-cell @ header-8, target-token @ header-8,
  target-cell @ cell-32 = if
    0 header-16,
  else
    target-cell @ cell-64 = if
      0 header-16, 0 header-32,
    then
  then
  mem-size @ header,
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

\ Align an address to a power of two
\ REMOVE AFTER COMPILING SUCCESSFULLY
: aligned-to ( addr1 pow2 -- addr2 ) swap 1 - swap 1 - or 1 + ;

\ Align code to a power of two
: align-code-to ( pow2 -- )
  code-buffer @ get-buffer-length tuck swap aligned-to
  swap - here over allot 2dup swap 0 fill over code-buffer @ append-buffer
  negate allot ;

\ Align code to the cell-size
: align-code ( -- ) cell align-code-to ;

\ Align code to 32 bits
: walign-code ( -- ) 4 align-code-to ;

\ Align code to 16 bits
: halign-code ( -- ) 2 align-code-to ;

\ Align a token in code
: align-code-token ( -- )
  target-token @ case
    token-16 of halign-code endof
    token-16-32 of halign-code endof
    token-32 of walign-code endof
  endcase ;

\ Align an offset a token in code
: aligned-code-token ( offset -- offset )
  target-token @ case
    token-16 of 2 aligned-to endof
    token-16-32 of 2 aligned-to endof
    token-32 of 4 aligned-to endof
  endcase ;

: token-8-16, ( token -- )
  dup $7f > if
    dup $ff and $80 or code-buffer @ append-byte-buffer
    7 rshift 1- $ff and code-buffer @ append-byte-buffer
  else
    code-buffer @ append-byte-buffer
  then ;

: token-16, ( token -- )
  halign-code $ffff and here h! here 2 code-buffer @ append-buffer ;

: token-16-32, ( token -- )
  halign-code
  dup $7fff > if
    dup $ffff and $8000 or here h! here 2
    code-buffer @ append-buffer
    15 rshift 1- $ffff and here h! here 2
    code-buffer @ append-buffer
  else
    here h! here 2 code-buffer @ append-buffer
  then ;

: token-32, ( token -- )
  walign-code $ffffffff and here w! here 4 code-buffer @ append-buffer ;

: token, ( token -- )
\  cr ." compiling token: " dup .
  target-token @ case
    token-8-16 of token-8-16, endof
    token-16 of token-16, endof
    token-16-32 of token-16-32, endof
    token-32 of token-32, endof
  endcase ;

: arg-8, ( value -- )
  $ff and here c! here 1 code-buffer @ append-buffer ;

: arg-16, ( value -- )
  halign-code $ffff and here h! here 2 code-buffer @ append-buffer ;

: arg-32, ( value -- )
  walign-code $ffffffff and here w! here 4 code-buffer @ append-buffer ;

: arg-64, ( value -- )
  align-code here ! here cell code-buffer @ append-buffer ;

: arg, ( value -- )
  target-cell @ case
    cell-16 of arg-16, endof
    cell-32 of arg-32, endof
    cell-64 of arg-64, endof
  endcase ;

: set-arg-16 ( value offset -- )
  swap $ffff and here h! here 2 rot code-buffer @ write-buffer ;

: set-arg-32 ( value offset -- )
  swap $ffffffff and here w! here 4 rot code-buffer @ write-buffer ;

: set-arg-64 ( value offset -- )
  swap here ! here cell rot code-buffer @ write-buffer ;

: set-arg ( value offset -- )
  target-cell @ case
    cell-16 of set-arg-16 endof
    cell-32 of set-arg-32 endof
    cell-64 of set-arg-64 endof
  endcase ;

variable current-token 89 current-token !

: next-token ( -- token ) current-token @ dup 1+ current-token ! ;

0 constant headers-end
1 constant colon-word
2 constant create-word

\ Get a reference
: get-ref ( -- ref ) code-buffer @ get-buffer-length ;

\ Get a cell-aligned reference
: get-align-ref ( -- ref ) align-code get-ref ;

\ Get a token-aligned reference
: get-token-align-ref ( -- ref ) align-code-token get-ref ;

: set-name-info ( addr bytes token flags -- )
  3 pick 3 pick type ." : " over (.) cr
  get-ref here name-offset ! 2 pick here name-length !
  here name-table-entry-size
  3 pick name-table-entry-size * name-table-offset @ +
  code-buffer @ write-buffer
  here info-flags ! dup dup 0> if 1- then here info-next !
  0 here info-start ! 0 here info-end !
  here info-table-entry-size
  rot info-table-entry-size * info-table-offset @ +
  code-buffer @ write-buffer
  set-data ;

: set-start ( offset token -- )
  swap here !
  here 1 cells rot info-table-entry-size * info-table-offset @ + info-start
  code-buffer @ write-buffer ;

: set-end ( offset token -- )
  swap here !
  here 1 cells rot info-table-entry-size * info-table-offset @ + info-end
  code-buffer @ write-buffer ;

: make-colon-word ( token name-addr name-length flags -- )
  colon-word header, 3 pick header,
  over get-ref + aligned-code-token dup >r header, 3 roll swap
  over >r set-name-info r> r> swap set-start align-code-token ;

: make-create-word ( token name-addr name-length flags -- )
  create-word header, 3 pick header,
  over get-ref + cell aligned-to header, 3 roll swap
  set-name-info align-code ;

: make-create-word-with-offset ( token name-addr name-length flags offset -- )
  create-word header, 4 pick header, header, 3 roll swap
  set-name-info ;

: end-headers ( -- )
  headers-end header, code-buffer @ get-buffer-length header, ;

: x-unable-to-open space ." unable to open file" cr ;
: x-unable-to-write space ." unable to write to file" cr ;

256 allocate-buffer constant backup-name-buffer

: get-backup-name ( name-addr name-length -- backup-addr backup-length )
  0 begin
    backup-name-buffer @ clear-buffer
    2 pick 2 pick backup-name-buffer @ append-buffer
    [char] . backup-name-buffer @ append-byte-buffer
    dup base @ >r 10 base ! format-unsigned
    backup-name-buffer @ append-buffer r> base !
    backup-name-buffer @ get-buffer open-rdonly /444 open 0 <> if
      close drop 1 + false
    else
      2drop 2drop backup-name-buffer @ get-buffer  true
    then
  until ;

: backup-file ( name-addr name-length -- )
  1024 allocate-buffer 2 pick 2 pick 2 pick read-file-into-buffer 0 <> if
    rot rot get-backup-name
    open-wronly open-creat or open-trunc or /666 open 0 <> if
      over get-buffer 2 pick wait-write-full 0 <> if
        drop close drop destroy-buffer
      else
        drop drop destroy-buffer ['] x-unable-to-write ?raise
      then
    else
      drop destroy-buffer ['] x-unable-to-open ?raise
    then
  else
    destroy-buffer 2drop
  then ;

: write-asm-to-file ( name-addr name-length -- )
  2dup backup-file
  end-headers open-wronly open-creat or open-trunc or /666 open
  averts x-unable-to-open
  header-buffer @ get-buffer 2 pick
  wait-write-full averts x-unable-to-write drop
  code-buffer @ get-buffer 2 pick
  wait-write-full averts x-unable-to-write drop
  storage-buffer @ get-buffer 2 pick
  wait-write-full averts x-unable-to-write drop
  close drop ;

: x-unable-to-read space ." unable to read file" cr ;

: add-source-to-storage ( name-addr name-length -- )
  1024 allocate-buffer rot rot 2 pick read-file-into-buffer 0 <> if
    dup get-buffer storage-buffer @ append-buffer
  else
    destroy-buffer ['] x-unable-to-read ?raise
  then
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
  over 1+ allocate-buffer [char] & over append-byte-buffer 3 roll 3 roll 2 pick
  append-buffer dup get-buffer create-with-name swap , destroy-buffer
  does> 5 token, @ arg, ;

: create-token ( c-addr bytes token -- )
  over 1+ allocate-buffer [char] ^ over append-byte-buffer 3 roll 3 roll 2 pick
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

: vm forth-wordlist buffer-wordlist hashforth-wordlist hashforth-asm-wordlist
  4 set-order ; immediate

: not-vm hashforth-asm-wordlist hashforth-wordlist buffer-wordlist
  forth-wordlist 4 set-order ; immediate

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
6 primitive (litc)
7 primitive (lith)
8 primitive (litw)
9 primitive (data)
10 primitive new-colon
11 primitive new-create
12 primitive set-does>
13 primitive finish
14 primitive execute
15 primitive drop
16 primitive dup
17 primitive swap
18 primitive over
19 primitive rot
20 primitive pick
21 primitive roll
22 primitive @
23 primitive !
24 primitive c@
25 primitive c!
26 primitive =
27 primitive <>
28 primitive <
29 primitive >
30 primitive u<
31 primitive u>
32 primitive not
33 primitive and
34 primitive or
35 primitive xor
36 primitive lshift
37 primitive rshift
38 primitive arshift
39 primitive +
40 primitive -
41 primitive *
42 primitive /
43 primitive mod
44 primitive u/
45 primitive umod
46 primitive mux
47 primitive /mux
48 primitive r@
49 primitive >r
50 primitive r>
51 primitive sp@
52 primitive sp!
53 primitive rp@
54 primitive rp!
55 primitive >body
56 primitive h@
57 primitive h!
58 primitive w@
59 primitive w!
60 primitive set-word-count
61 primitive sys
62 primitive d+
63 primitive d-
64 primitive d*
65 primitive d/
66 primitive dmod
67 primitive du/
68 primitive dumod
69 primitive dnot
70 primitive dand
71 primitive dor
72 primitive dxor
73 primitive dlshift
74 primitive drshift
75 primitive darshift
76 primitive d<
77 primitive d>
78 primitive d=
79 primitive d<>
80 primitive du<
81 primitive du>
82 primitive */
83 primitive *rshift
84 primitive u*/
85 primitive u*rshift
86 primitive ulshift/
87 primitive */mod
88 primitive u*/mod

name-table-offset @ define-word-created-with-offset name-table
info-table-offset @ define-word-created-with-offset info-table

: end-word ( -- ) vm exit end not-vm get-ref current-token @ 1 - set-end ;

: lit ( x -- )
[ target-token @ token-8-16 = ] [if]
  dup 127 <= over -128 >= and if
    vm (litc) not-vm arg-8,
  else
[then]
[ target-token @ dup token-8-16 = over token-16 = or swap token-16-32 = or ]
[if]  
    dup 32767 <= over -32768 >= and if
      vm (lith) not-vm arg-16,
    else
[then]
      dup 2147483647 <= over -2147483648 >= and if
	vm (litw) not-vm arg-32,
      else
	vm (lit) not-vm arg,
      then
[ target-token @ dup token-8-16 = over token-16 = or swap token-16-32 = or ]
[if]  
    then
[then]
[ target-token @ token-8-16 = ] [if]
  then
[then]
  ;

: +branch-back ( x -- ) vm branch not-vm arg, ;

: +0branch-back ( x -- ) vm 0branch not-vm arg, ;

: +if ( -- forward-ref ) vm 0branch not-vm get-align-ref 0 arg, ;

: +else ( forward-ref -- forward-ref )
  vm branch not-vm get-align-ref 0 arg, swap get-token-align-ref swap set-arg ;

: +then ( forward-ref -- ) get-token-align-ref swap set-arg ;

: +begin ( -- backward-ref ) get-token-align-ref ;

: +again ( backward-ref -- ) +branch-back ;

: +until ( backward-ref -- ) +0branch-back ;

: +while ( -- forward-ref ) vm 0branch not-vm get-align-ref 0 arg, ;

: +repeat ( backward-ref forward-ref -- )
  swap +branch-back get-align-ref swap set-arg ;

: +data ( c-addr bytes -- )
  vm (data) not-vm dup arg, tuck set-data vm lit not-vm ;

base ! set-current set-order
