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

GET-ORDER GET-CURRENT BASE @

DECIMAL
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

REQUIRE buffer.fs

WORDLIST CONSTANT HASHFORTH-WORDLIST
WORDLIST CONSTANT HASHFORTH-ASM-WORDLIST

FORTH-WORDLIST HASHFORTH-WORDLIST 2 SET-ORDER
HASHFORTH-WORDLIST SET-CURRENT

VARIABLE HEADER-BUFFER
VARIABLE CODE-BUFFER

0 CONSTANT CELL-16
1 CONSTANT CELL-32
2 CONSTANT CELL-64

0 CONSTANT TOKEN-8-16
1 CONSTANT TOKEN-16
2 CONSTANT TOKEN-16-32
3 CONSTANT TOKEN-32

VARIABLE TARGET-CELL CELL-64 TARGET-CELL !
VARIABLE TARGET-TOKEN TOKEN-32 TARGET-TOKEN !

: INIT-ASM ( cell-type token-type -- )
  HEADER-BUFFER @ IF
    HEADER-BUFFER @ CLEAR-BUFFER
  ELSE
    65536 NEW-BUFFER HEADER-BUFFER !
  THEN
  CODE-BUFFER @ IF
    CODE-BUFFER @ CLEAR-BUFFER
  ELSE
    65536 NEW-BUFFER CODE-BUFFER !
  THEN
  TARGET-TOKEN ! TARGET-CELL !
  TARGET-CELL @ HEADER-BUFFER @ APPEND-BYTE-BUFFER
  TARGET-TOKEN @ HEADER-BUFFER @ APPEND-BYTE-BUFFER ;

: TARGET-CELL-SIZE ( -- bytes )
  TARGET-CELL @ CASE
    CELL-16 OF 2 ENDOF
    CELL-32 OF 4 ENDOF
    CELL-64 OF 8 ENDOF
  ENDCASE ;

: TOKEN-8-16, ( token -- )
  DUP $7F > IF
    DUP $FF AND $80 OR CODE-BUFFER @ APPEND-BYTE-BUFFER
    7 RSHIFT $FF AND CODE-BUFFER @ APPEND-BYTE-BUFFER
  ELSE
    CODE-BUFFER @ APPEND-BYTE-BUFFER
  THEN ;

: TOKEN-16, ( token -- )
  $FFFF AND HERE H! HERE HALFWORD-SIZE CODE-BUFFER @ APPEND-BUFFER ;

: TOKEN-16-32, ( token -- )
  DUP $7FFF > IF
    DUP $FFFF AND $8000 OR HERE H! HERE HALFWORD-SIZE
    CODE-BUFFER @ APPEND-BUFFER
    15 RSHIFT $FFFF AND HERE H! HERE HALFWORD-SIZE CODE-BUFFER @ APPEND-BUFFER
  ELSE
    HERE H! HERE HALFWORD-SIZE CODE-BUFFER @ APPEND-BUFFER
  THEN ;

: TOKEN-32, ( token -- )
  $FFFFFFFF AND HERE W! HERE WORD-SIZE CODE-BUFFER @ APPEND-BUFFER ;

: TOKEN, ( token -- )
  cr ." compiling token: " dup .
  TARGET-TOKEN @ CASE
    TOKEN-8-16 OF TOKEN-8-16, ENDOF
    TOKEN-16 OF TOKEN-16, ENDOF
    TOKEN-16-32 OF TOKEN-16-32, ENDOF
    TOKEN-32 OF TOKEN-32, ENDOF
  ENDCASE ;

: ARG-16, ( value -- )
  $FFFF AND HERE H! HERE HALFWORD-SIZE CODE-BUFFER @ APPEND-BUFFER ;

: ARG-32, ( value -- )
  $FFFFFFFF AND HERE W! HERE WORD-SIZE CODE-BUFFER @ APPEND-BUFFER ;

: ARG-64, ( value -- )
  HERE ! HERE CELL-SIZE CODE-BUFFER @ APPEND-BUFFER ;

: ARG, ( value -- )
  TARGET-CELL @ CASE
    CELL-16 OF ARG-16, ENDOF
    CELL-32 OF ARG-32, ENDOF
    CELL-64 OF ARG-64, ENDOF
  ENDCASE ;

: SET-ARG-16 ( value offset -- )
  SWAP $FFFF AND HERE H! HERE HALFWORD-SIZE ROT CODE-BUFFER @ WRITE-BUFFER ;

: SET-ARG-32 ( value offset -- )
  SWAP $FFFFFFFF AND HERE W! HERE WORD-SIZE ROT CODE-BUFFER @ WRITE-BUFFER ;

: SET-ARG-64 ( value offset -- )
  SWAP HERE ! HERE CELL-SIZE ROT CODE-BUFFER @ WRITE-BUFFER ;

: SET-ARG ( value offset -- )
  TARGET-CELL @ CASE
    CELL-16 OF SET-ARG-16 ENDOF
    CELL-32 OF SET-ARG-32 ENDOF
    CELL-64 OF SET-ARG-64 ENDOF
  ENDCASE ;

VARIABLE CURRENT-TOKEN 72 CURRENT-TOKEN !

: NEXT-TOKEN ( -- ) CURRENT-TOKEN @ DUP 1+ CURRENT-TOKEN ! ;

: HEADER-8, ( value -- )
  $FF AND HEADER-BUFFER @ APPEND-BYTE-BUFFER ;

: HEADER-16, ( value -- )
  $FFFF AND HERE H! HERE HALFWORD-SIZE HEADER-BUFFER @ APPEND-BUFFER ;

: HEADER-32, ( value -- )
  $FFFFFFFF AND HERE W! HERE WORD-SIZE HEADER-BUFFER @ APPEND-BUFFER ;

: HEADER-64, ( value -- )
  HERE ! HERE CELL-SIZE HEADER-BUFFER @ APPEND-BUFFER ;

: HEADER, ( value -- )
  TARGET-CELL @ CASE
    CELL-16 OF HEADER-16, ENDOF
    CELL-32 OF HEADER-32, ENDOF
    CELL-64 OF HEADER-64, ENDOF
  ENDCASE ;

: HEADER-ARRAY, ( c-addr bytes -- ) HEADER-BUFFER @ APPEND-BUFFER ;

0 CONSTANT HEADERS-END
1 CONSTANT COLON-WORD
2 CONSTANT CREATE-WORD

: MAKE-COLON-WORD ( token name-addr name-length offset flags -- )
  cr ." colon word " COLON-WORD HEADER-8, 4 ROLL ." token: " dup . HEADER, ." flags: " dup . HEADER, ." offset: " dup . HEADER, ." name: " 2dup type DUP HEADER,
  HEADER-ARRAY, ;

: MAKE-CREATE-WORD ( token name-addr name-length offset flags -- )
   cr ." create word " CREATE-WORD HEADER-8, 4 ROLL ." token: " dup . HEADER, ." flags: " dup . HEADER, ." offset: " dup . HEADER, ." name: " 2dup type DUP HEADER,
  HEADER-ARRAY, ;

: END-HEADERS ( -- ) HEADERS-END HEADER-8, ;

: WRITE-ASM-TO-FILE ( name-addr name-length -- )
  END-HEADERS W/O CREATE-FILE ABORT" unable to open file"
  HEADER-BUFFER @ GET-BUFFER 2 PICK WRITE-FILE ABORT" unable to write file"
  CODE-BUFFER @ GET-BUFFER 2 PICK WRITE-FILE ABORT" unable to write file"
  CLOSE-FILE ;

: SET-DATA ( c-addr bytes -- ) CODE-BUFFER @ APPEND-BUFFER ;

: SET-FILL-DATA ( bytes c -- )
  OVER ALLOCATE! DUP 3 PICK 3 ROLL FILL TUCK SWAP SET-DATA FREE! ;

: SET-CELL-DATA ( x -- ) HERE ! HERE CELL-SIZE SET-DATA ;

0 CONSTANT NO-FLAG
1 CONSTANT IMMEDIATE-FLAG
2 CONSTANT COMPILE-ONLY-FLAG
4 CONSTANT HIDDEN-FLAG

: 3DUP ( x1 x2 x3 -- x1 x2 x3 x1 x2 x3 ) 2 PICK 2 PICK 2 PICK ;

\ : CREATE-COMPILE ( c-addr bytes token -- )
\   OVER 1+ NEW-BUFFER [CHAR] . OVER APPEND-BYTE-BUFFER 3 ROLL 3 ROLL 2 PICK
\   APPEND-BUFFER DUP GET-BUFFER CREATE-WITH-NAME SWAP , DESTROY-BUFFER
\   DOES> @ TOKEN, ;

: CREATE-COMPILE ( c-addr bytes token -- )
  ROT ROT CREATE-WITH-NAME , DOES> @ TOKEN, ;

: CREATE-TOKEN-LIT ( c-addr bytes token -- )
  OVER 1+ NEW-BUFFER [CHAR] & OVER APPEND-BYTE-BUFFER 3 ROLL 3 ROLL 2 PICK
  APPEND-BUFFER DUP GET-BUFFER CREATE-WITH-NAME SWAP , DESTROY-BUFFER
  DOES> 5 TOKEN, @ ARG, ;

: CREATE-TOKEN ( c-addr bytes token -- )
  OVER 1+ NEW-BUFFER [CHAR] ^ OVER APPEND-BYTE-BUFFER 3 ROLL 3 ROLL 2 PICK
  APPEND-BUFFER DUP GET-BUFFER CREATE-WITH-NAME SWAP , DESTROY-BUFFER
  DOES> @ ;

: CREATE-WORDS ( c-addr bytes token )
  3DUP CREATE-COMPILE 3DUP CREATE-TOKEN-LIT CREATE-TOKEN ;

: CREATE-TOKEN-AND-LIT-TOKEN ( c-addr bytes token )
  3DUP CREATE-TOKEN-LIT CREATE-TOKEN ;

: PRIMITIVE ( "name" token -- ) PARSE-NAME ROT CREATE-WORDS ;

: GET-REF ( -- ref ) CODE-BUFFER @ GET-BUFFER-LENGTH ;

: VM FORTH-WORDLIST HASHFORTH-WORDLIST HASHFORTH-ASM-WORDLIST
  3 SET-ORDER ; IMMEDIATE

: NOT-VM HASHFORTH-ASM-WORDLIST HASHFORTH-WORDLIST FORTH-WORDLIST
  3 SET-ORDER ; IMMEDIATE

NOT-VM HASHFORTH-ASM-WORDLIST SET-CURRENT

: DEFINE-WORD ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT
  GET-REF NO-FLAG MAKE-COLON-WORD ;

: DEFINE-WORD-IMMEDIATE ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF IMMEDIATE-FLAG MAKE-COLON-WORD ;

: DEFINE-WORD-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF COMPILE-ONLY-FLAG MAKE-COLON-WORD ;

: DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF IMMEDIATE-FLAG COMPILE-ONLY-FLAG OR MAKE-COLON-WORD ;

: DEFINE-WORD-CREATED ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF NO-FLAG MAKE-CREATE-WORD ;

: DEFINE-WORD-CREATED-IMMEDIATE ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF IMMEDIATE-FLAG MAKE-CREATE-WORD ;

: DEFINE-WORD-CREATED-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF COMPILE-ONLY-FLAG MAKE-CREATE-WORD ;

: DEFINE-WORD-CREATED-IMMEDIATE-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-WORDS ROT ROT 
  GET-REF IMMEDIATE-FLAG COMPILE-ONLY-FLAG OR MAKE-CREATE-WORD ;

: NON-DEFINE-WORD ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF NO-FLAG MAKE-COLON-WORD ;

: NON-DEFINE-WORD-IMMEDIATE ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF IMMEDIATE-FLAG MAKE-COLON-WORD ;

: NON-DEFINE-WORD-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF COMPILE-ONLY-FLAG MAKE-COLON-WORD ;

: NON-DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF IMMEDIATE-FLAG COMPILE-ONLY-FLAG OR MAKE-COLON-WORD ;

: NON-DEFINE-WORD-CREATED ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF NO-FLAG MAKE-CREATE-WORD ;

: NON-DEFINE-WORD-CREATED-IMMEDIATE ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF IMMEDIATE-FLAG MAKE-CREATE-WORD ;

: NON-DEFINE-WORD-CREATED-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF COMPILE-ONLY-FLAG MAKE-CREATE-WORD ;

: NON-DEFINE-WORD-CREATED-IMMEDIATE-COMPILE-ONLY ( "name" -- )
  PARSE-NAME NEXT-TOKEN 3DUP CREATE-TOKEN-AND-LIT-TOKEN ROT ROT 
  GET-REF IMMEDIATE-FLAG COMPILE-ONLY-FLAG OR MAKE-CREATE-WORD ;

0 PRIMITIVE END
1 PRIMITIVE NOP
2 PRIMITIVE EXIT
3 PRIMITIVE BRANCH
4 PRIMITIVE 0BRANCH
5 PRIMITIVE LIT
6 PRIMITIVE (DATA)
7 PRIMITIVE NEW-COLON
8 PRIMITIVE NEW-CREATE
9 PRIMITIVE SET-DOES>
10 PRIMITIVE FINISH
11 PRIMITIVE EXECUTE
12 PRIMITIVE DROP
13 PRIMITIVE DUP
14 PRIMITIVE SWAP
15 PRIMITIVE ROT
16 PRIMITIVE PICK
17 PRIMITIVE ROLL
18 PRIMITIVE @
19 PRIMITIVE !
20 PRIMITIVE C@
21 PRIMITIVE C!
22 PRIMITIVE =
23 PRIMITIVE <>
24 PRIMITIVE <
25 PRIMITIVE >
26 PRIMITIVE U<
27 PRIMITIVE U>
28 PRIMITIVE NOT
29 PRIMITIVE AND
30 PRIMITIVE OR
31 PRIMITIVE XOR
32 PRIMITIVE LSHIFT
33 PRIMITIVE RSHIFT
34 PRIMITIVE ARSHIFT
35 PRIMITIVE +
36 PRIMITIVE -
37 PRIMITIVE *
38 PRIMITIVE /
39 PRIMITIVE MOD
40 PRIMITIVE U/
41 PRIMITIVE UMOD
42 PRIMITIVE R@
43 PRIMITIVE >R
44 PRIMITIVE R>
45 PRIMITIVE HERE
46 PRIMITIVE HERE!
47 PRIMITIVE SP@
48 PRIMITIVE SP!
49 PRIMITIVE RP@
50 PRIMITIVE RP!
51 PRIMITIVE >BODY
52 PRIMITIVE WORD>NAME
53 PRIMITIVE NAME>WORD
54 PRIMITIVE WORD>NEXT
55 PRIMITIVE WORDLIST>FIRST
56 PRIMITIVE GET-CURRENT
57 PRIMITIVE SET-CURRENT
58 PRIMITIVE WORD>FLAGS
59 PRIMITIVE FLAGS>WORD
60 PRIMITIVE WORDLIST
61 PRIMITIVE HALF-TOKEN-SIZE
62 PRIMITIVE FULL-TOKEN-SIZE
63 PRIMITIVE TOKEN-FLAG-BIT
64 PRIMITIVE CELL-SIZE
65 PRIMITIVE H@
66 PRIMITIVE H!
67 PRIMITIVE W@
68 PRIMITIVE W!
69 PRIMITIVE TYPE
70 PRIMITIVE KEY
71 PRIMITIVE ACCEPT

: END-WORD ( -- ) VM EXIT END NOT-VM ;

: LIT ( x -- ) VM LIT NOT-VM ARG, ;

: BACK-REF ( "name" -- ) CREATE GET-REF , DOES> @ ;

: +BRANCH-FORE ( "name" -- )
  CREATE VM BRANCH NOT-VM GET-REF , 0 ARG, DOES> @ GET-REF SWAP SET-ARG ;

: +0BRANCH-FORE ( "name" -- )
  CREATE VM 0BRANCH NOT-VM GET-REF , 0 ARG, DOES> @ GET-REF SWAP SET-ARG ;

: +BRANCH-BACK ( x -- ) VM BRANCH NOT-VM ARG, ;

: +0BRANCH-BACK ( x -- ) VM 0BRANCH NOT-VM ARG, ;

: +IF ( -- forward-ref ) VM 0BRANCH NOT-VM GET-REF 0 ARG, ;

: +ELSE ( forward-ref -- forward-ref )
  VM BRANCH NOT-VM GET-REF 0 ARG, SWAP GET-REF SWAP SET-ARG ;

: +THEN ( forward-ref -- ) GET-REF SWAP SET-ARG ;

: +BEGIN ( -- backward-ref ) GET-REF ;

: +AGAIN ( backward-ref -- ) +BRANCH-BACK ;

: +UNTIL ( backward-ref -- ) +0BRANCH-BACK ;

: +WHILE ( -- forward-ref ) VM 0BRANCH NOT-VM GET-REF 0 ARG, ;

: +REPEAT ( backward-ref forward-ref -- )
  SWAP +BRANCH-BACK GET-REF SWAP SET-ARG ;

: +DATA ( c-addr bytes -- )
  VM (DATA) NOT-VM DUP ARG, TUCK SET-DATA VM LIT NOT-VM ;

BASE ! SET-CURRENT SET-ORDER
