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

DECIMAL
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT BUILD-IMAGE-WORDLIST
WORDLIST CONSTANT BUILD-IMAGE-ASM-WORDLIST

REQUIRE src/asm/asm.fs

BUILD-IMAGE-WORDLIST SET-CURRENT

: VM FORTH-WORDLIST HASHFORTH-WORDLIST HASHFORTH-ASM-WORDLIST
  BUILD-IMAGE-WORDLIST BUILD-IMAGE-ASH-WORDLIST 5 SET-ORDER ;

: NOT-VM HASHFORTH-ASM-WORDLIST BUILD-IMAGE-ASM-WORDLIST HASHFORTH-WORDLIST
  BUILD-IMAGE-WORDLIST FORTH-WORDLIST 5 SET-ORDER

NOT-VM BUILD-IMAGE-ASM-WORDLIST SET-CURRENT

CELL-64 TOKEN-16-32 INIT-ASM

: +TRUE -1 VM LIT NOT-VM ;

: +FALSE 0 VM LIT NOT-VM ;

VM

DEFINE-WORD TRUE +TRUE END-WORD

DEFINE-WORD FALSE +FALSE END-WORD

DEFINE-WORD CELL+ ( u -- u ) CELL-SIZE + END-WORD

DEFINE-WORD CELLS ( u -- u ) CELL-SIZE * END-WORD

DEFINE-WORD NEGATE ( n -- n ) 1 LIT - NOT END-WORD

DEFINE-WORD ALLOT ( u -- ) HERE ADD HERE! END-WORD

DEFINE-WORD , ( x -- ) HERE ! CELL-SIZE ALLOT END-WORD

DEFINE-WORD C, ( c -- ) HERE C! 1 LIT ALLOT END-WORD

DEFINE-WORD H, ( c -- ) HERE H! 2 LIT ALLOT END-WORD

DEFINE-WORD W, ( c -- ) HERE W! 4 LIT ALLOT END-WORD

DEFINE-WORD COMPILE, ( token -- )
  TARGET-TOKEN @ TOKEN-8-16 = LIT +IF
    DUP $80 LIT U< +IF
      C, BRANCH-FORE =COMPILE,-8-16-LO
    +ELSE
      DUP $7F LIT AND $80 LIT OR C, 7 LIT RSHIFT
      $FF LIT AND C,
    +THEN
  +ELSE
    TARGET-TOKEN @ TOKEN-16 = LIT +IF
      $FFFF LIT AND H,
      BRANCH-FORE =COMPILE,-16-END
    +ELSE
      TARGET-TOKEN @ TOKEN-16-32 = LIT +IF
        DUP $8000 LIT U< +IF
          H, BRANCH-FORE =COMPILE,-16-32-LO
        +ELSE
          DUP $7FFF LIT AND $8000 LIT OR H, 15 LIT RSHIFT
          $FFFF LIT AND H,
	+THEN
      +ELSE ( TARGET-TOKEN @ TOKEN-32 = )
        $FFFFFFFF LIT AND W,
      +THEN
    +THEN
  +THEN
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY IF ( f -- ) ( Compile-time: -- fore-ref )
  &0BRANCH LIT COMPILE, HERE 0 LIT ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ELSE ( -- )
  ( Compile-time: fore-ref -- fore-ref )
  &BRANCH LIT COMPILE, HERE 0 LIT , SWAP HERE SWAP !
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY THEN ( -- ) ( Compile-time: fore-ref -- )
  HERE SWAP !
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY BEGIN ( -- ) ( Compile-time: -- back-ref )
  HERE
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY AGAIN ( -- ) ( Compile-time: back-ref -- )
  &BRANCH LIT COMPILE, ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY UNTIL ( -- ) ( Compile-time: back-ref -- )
  &0BRANCH LIT COMPILE, ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY WHILE ( -- ) ( Compile_time: -- fore-ref )
  &0BRANCH LIT COMPILE, HERE 0 LIT ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY REPEAT ( -- )
  ( Compile-time: back-ref fore-ref -- )
  SWAP &BRANCH LIT COMPILE, , HERE SWAP !
END-WORD

DEFINE-WORD OVER ( x1 x2 -- x1 x2 x1 ) 1 LIT PICK END-WORD

DEFINE-WORD 2DUP ( x1 x2 -- x1 x2 x1 x2 ) OVER OVER END-WORD

DEFINE-WORD 2DROP ( x1 x2 -- ) DROP DROP END-WORD

DEFINE-WORD <= ( n1 n2 -- flag ) DUP DUP = ROT ROT < OR END-WORD

DEFINE-WORD >= ( n1 n2 -- flag ) DUP DUP = ROT ROT > OR END-WORD

DEFINE-WORD UPCASE-CHAR ( c -- c )
  DUP CHAR a >= OVER CHAR z <= AND +IF CHAR a - THEN
END-WORD

DEFINE-WORD EQUAL-CASE-CHARS ( c-addr1 c-addr2 bytes -- matches )
  BEGIN DUP 0 LIT >
    2 LIT PICK C@ UPCASE-CHAR 2 LIT PICK C@ UPCASE-CHAR <> +IF
      2DROP DROP +FALSE EXIT
    +THEN
    1 LIT - ROT 1 LIT + ROT 1 LIT + ROT
  REPEAT
  2DROP DROP +TRUE
END-WORD

DEFINE-WORD EQUAL-NAME ( c-addr1 bytes1 c-addr2 bytes2 -- matches )
  DUP 3 LIT PICK = +IF
    DROP SWAP EQUAL-CASE-CHARS
  +ELSE
    2DROP 2DROP +FALSE
  +THEN
END-WORD

DEFINE-WORD SEARCH-WORDLIST ( c-addr bytes wid -- xt found )
  WORDLIST>FIRST +BEGIN DUP 0 LIT <> +WHILE
    2 LIT PICK 2 LIT PICK 2 LIT PICK WORD>NAME EQUAL-NAME +IF
      ROT ROT 2DROP +TRUE EXIT
    +THEN
    WORD>NEXT
  +REPEAT
  2DROP DROP 0 LIT +FALSE
END-WORD

NOT-VM 128 CONSTANT MAX-WORDLIST-COUNT VM

DEFINE-WORD-CREATED WORDLIST-ARRAY
NOT-VM TARGET-CELL-SIZE MAX-WORDLIST-COUNT * 0 SET-FILL-DATA VM

DEFINE-WORD-CREATED WORDLIST-COUNT
1 SET-CELL-DATA

DEFINE-WORD SEARCH-WORDLISTS ( c-addr bytes -- xt found )
  0 LIT +BEGIN DUP WORDLIST-COUNT @ < +WHILE
    DUP CELL-SIZE * WORDLIST-ARRAY + @ 3 LIT PICK 3 LIT PICK ROT
    SEARCH-WORDLIST +IF
      SWAP DROP SWAP DROP SWAP DROP +TRUE EXIT
    +ELSE
      DROP 1 LIT +
    +THEN
  +REPEAT
  2DROP DROP 0 LIT +FALSE
END-WORD

DEFINE-WORD-CREATED INPUT
0 SET-CELL-DATA

DEFINE-WORD-CREATED INPUT#
0 SET-CELL-DATA

DEFINE-WORD-CREATED >IN
0 SET-CELL-DATA

NOT-VM 1024 CONSTANT TIB-SIZE VM

DEFINE-WORD-CREATED TIB
TIB-SIZE 0 SET-FILL-DATA

DEFINE-WORD BL $20 LIT END-WORD

DEFINE-WORD NEWLINE $0A LIT END-WORD

DEFINE-WORD TAB $09 LIT END-WORD

DEFINE-WORD INPUT-LEFT? ( -- f ) >IN @ INPUT# @ < END-WORD

DEFINE-WORD INPUT-NOT-WS? ( -- f )
  INPUT @ >IN @ + @
  DUP BL <> OVER NEWLINE <> AND OVER TAB <> AND  
END-WORD

DEFINE-WORD ADVANCE->IN ( -- ) >IN @ 1 LIT + >IN ! END-WORD

DEFINE-WORD ADVANCE-TO-NAME-START ( -- found )
  +BEGIN
    INPUT-LEFT? +IF
      INPUT-NOT-WS? +IF +TRUE +TRUE +ELSE ADVANCE->IN +FALSE +THEN
    +ELSE
      +FALSE +TRUE
    +THEN
  +UNTIL
END-WORD

DEFINE-WORD ADVANCE-TO-NAME-END ( -- )
  +BEGIN
    INPUT-LEFT? +IF
      INPUT-NOT-WS? +IF ADVANCE->IN +FALSE +ELSE +TRUE +THEN
    +ELSE
      +TRUE
    +THEN
  +UNTIL
END-WORD

DEFINE-WORD PARSE-NAME ( -- c-addr bytes )
  ADVANCE-TO-NAME-START +IF
    >IN @ ADVANCE-TO-NAME-END INPUT @ >IN @ + >IN @ SWAP -
  +ELSE
    0 LIT 0 LIT
  +THEN
END-WORD

DEFINE-WORD HANDLE-NO-PARSE-FOUND ( -- ) END-WORD

DEFINE-WORD HANDLE-NO-WORD-FOUND ( c-addr bytes -- ) 2DROP END-WORD

DEFINE-WORD ' ( -- xt )
  PARSE-NAME DUP 0 LIT <> +IF
    DUP SEARCH-WORDLISTS +IF
      ROT ROT 2DROP
    +ELSE
      DROP HANDLE-NO-WORD-FOUND
    +THEN
  +ELSE
    2DROP HANDLE-NO-PARSE-FOUND
  +THEN
END-WORD

DEFINE-WORD-CREATED STATE
0 SET-CELL-DATA

DEFINE-WORD-IMMEDIATE ; ( -- ) +FALSE STATE ! ; END-WORD

DEFINE-WORD : ( -- )
  PARSE-NAME DUP 0 LIT <> +IF
    TRUE STATE ! NEW-COLON NAME>WORD
  +ELSE
    2DROP HANDLE-NO-PARSE-FOUND
  +THEN
END-WORD

DEFINE-WORD :NONAME ( -- )
  TRUE STATE ! NEW-COLON
END-WORD

DEFINE-WORD CREATE ( -- )
  PARSE-NAME DUP 0 LIT <> +IF
    TRUE STATE ! NEW-CREATE NAME>WORD
  +ELSE
    2DROP HANDLE-NO-PARSE-FOUND
  +THEN
END-WORD

DEFINE-WORD-CREATE BASE
10 SET-CELL-DATA

DEFINE-WORD ADVANCE-STRING ( c-addr1 bytes1 -- c-addr2 bytes2 )
  1 LIT - SWAP 1 LIT + SWAP
END-WORD

DEFINE-WORD PARSE-BASE ( c-addr1 bytes1 -- c-addr2 bytes2 base )
  DUP 0 LIT > IF
    OVER C@ DUP CHAR $ LIT = +IF DROP ADVANCE-STRING 16 LIT
    +ELSE DUP CHAR # LIT = +IF DROP ADVANCE-STRING 10 LIT
    +ELSE DUP CHAR / LIT = +IF DROP ADVANCE-STRING 8 LIT
    +ELSE CHAR % LIT = +IF ADVANCE-STRING 2 LIT
    +ELSE BASE @
    +THEN +THEN +THEN +THEN
  ELSE
    BASE @
  THEN
END-WORD

DEFINE-WORD PARSE-DIGIT-0-9 ( base c -- digit matches )
  CHAR 0 LIT - DUP ROT < +IF +TRUE +ELSE DROP 0 LIT +FALSE +THEN
END-WORD

DEFINE-WORD PARSE-DIGIT-A-Z ( base c -- digit matches )
  CHAR A LIT - 10 LIT + DUP ROT < +IF +TRUE
  +ELSE DROP 0 LIT +FALSE +THEN
END-WORD

DEFINE-WORD PARSE-DIGIT ( base c -- digit matches )
  DUP CHAR 0 LIT >= OVER CHAR 9 LIT <= AND +IF
    PARSE-DIGIT-0-9
  +ELSE
    UPCASE-CHAR CHAR A LIT >= OVER CHAR Z LIT <= AND +IF
      PARSE-DIGIT-A-Z
    +ELSE
      2DROP 0 LIT +FALSE
    +THEN
  +THEN
END-WORD

DEFINE-WORD PARSE-DIGITS ( base c-addr bytes -- n matches )
  0 LIT +BEGIN OVER 0 LIT > +WHILE
    3 LIT PICK 3 LIT PICK C@ PARSE-DIGIT +IF
      SWAP 4 LIT PICK * + ROT 1 LIT + ROT 1 LIT - ROT
    +ELSE
      2DROP 2DROP DROP 0 LIT +FALSE EXIT
  +REPEAT
  SWAP DROP SWAP DROP SWAP DROP +TRUE
END_WORD

DEFINE-WORD PARSE-NUMBER ( c-addr bytes -- n matches )
  PARSE-BASE ROT ROT DUP 0 LIT U> +IF
    OVER C@ CHAR - LIT = IF
      1 LIT - SWAP 1 LIT + SWAP DUP 0 LIT U> +IF
        PARSE-DIGITS DUP +IF
	  DROP NEGATE +TRUE
	+THEN
      +ELSE
        2DROP DROP 0 LIT +FALSE
      +THEN
    +ELSE
      PARSE-DIGITS
    +THEN
  +ELSE
    2DROP DROP 0 LIT +FALSE
  +THEN
END-WORD

DEFINE-WORD-IMMEDIATE [ ( -- ) +FALSE STATE ! END-WORD

DEFINE-WORD-IMMEDIATE ] ( -- ) +TRUE STATE ! END-WORD

DEFINE HANDLE-COMPILE-ONLY-ERROR ( c-addr bytes -- ) 2DROP END-WORD

DEFINE HANDLE-PARSE-ERROR ( c-addr bytes -- ) 2DROP END-WORD

DEFINE-WORD COMPILE-WORD ( c-addr bytes xt -- error )
  ROT ROT 2DROP DUP WORD>FLAGS IMMEDIATE-FLAG LIT AND +IF
    EXECUTE
  +ELSE
    COMPILE,
  +THEN
END-WORD

DEFINE-WORD INTERPRET-WORD ( c-addr bytes xt -- error )
  DUP WORD>FLAGS COMPILE-ONLY-FLAG LIT AND +IF
    DROP HANDLE-COMPILE-ONLY-ERROR +TRUE
  +ELSE
    ROT ROT 2DROP EXECUTE
  +THEN
END-WORD

DEFINE-WORD HANDLE-NUMBER ( c-addr bytes -- error )
  2DUP PARSE-NUMBER +IF
    ROT ROT 2DROP
    STATE @ +IF
      &LIT COMPILE, ,
    +THEN
    +FALSE
  +ELSE
    DROP HANDLE-PARSE-ERROR +TRUE
  +THEN
END-WORD

DEFINE-WORD INTERPRET ( -- )
  +BEGIN
    PARSE-NAME DUP 0 LIT U> +IF
      2DUP SEARCH-WORDLISTS +IF
        STATE @ +IF
	  COMPILE-WORD
	+ELSE
          INTERPRET-WORD
	+THEN
      +ELSE
        HANDLE-NUMBER
      +THEN
    +ELSE
      +TRUE
    +THEN
  +UNTIL
END-WORD

DEFINE-WORD REFILL-TERMINAL
  \ Add more here
END-WORD

DEFINE-WORD-CREATED 'REFILL
0 SET-CELL-DATA

DEFINE-WORD-CREATED SBASE
0 SET-CELL-DATA

DEFINE-WORD-CREATED RBASE
0 SET-CELL-DATA

DEFINE-WORD BOOT ( -- )
  SP@ SBASE ! RP@ RBASE! TIB INPUT !
  &REFILL-TERMINAL 'REFILL !
END-WORD

S" cell_64_token_16_32.image" WRITE-ASM-TO-FILE
