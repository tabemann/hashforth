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

HASHFORTH-ASM-WORDLIST BUILD-IMAGE-ASM-WORDLIST HASHFORTH-WORDLIST
BUILD-IMAGE-WORDLIST FORTH-WORDLIST 5 SET-ORDER
BUILD-IMAGE-WORDLIST SET-CURRENT

: VM FORTH-WORDLIST HASHFORTH-WORDLIST HASHFORTH-ASM-WORDLIST
  BUILD-IMAGE-WORDLIST BUILD-IMAGE-ASM-WORDLIST 5 SET-ORDER ; IMMEDIATE

: NOT-VM HASHFORTH-ASM-WORDLIST BUILD-IMAGE-ASM-WORDLIST HASHFORTH-WORDLIST
  BUILD-IMAGE-WORDLIST FORTH-WORDLIST 5 SET-ORDER ; IMMEDIATE

BUILD-IMAGE-ASM-WORDLIST SET-CURRENT

CELL-64 TOKEN-16-32 1024 1024 * 16 * INIT-ASM

: +TRUE -1 VM LIT NOT-VM ;

: +FALSE 0 VM LIT NOT-VM ;

VM

DEFINE-WORD TRUE +TRUE END-WORD

DEFINE-WORD FALSE +FALSE END-WORD

DEFINE-WORD CELL+ ( u -- u ) CELL-SIZE + END-WORD

DEFINE-WORD CELLS ( u -- u ) CELL-SIZE * END-WORD

DEFINE-WORD NEGATE ( n -- n ) 1 LIT - NOT END-WORD

DEFINE-WORD-CREATED USER-SPACE-CURRENT
0 SET-CELL-DATA

DEFINE-WORD HERE ( -- addr ) USER-SPACE-CURRENT @ END-WORD

DEFINE-WORD HERE! ( addr -- ) USER-SPACE-CURRENT ! END-WORD

DEFINE-WORD ALLOT ( u -- ) HERE + HERE! END-WORD

DEFINE-WORD BL $20 LIT END-WORD

DEFINE-WORD NEWLINE $0A LIT END-WORD

DEFINE-WORD TAB $09 LIT END-WORD

DEFINE-WORD EMIT ( c -- ) HERE C! HERE 1 LIT TYPE END-WORD

DEFINE-WORD SPACE ( -- ) BL EMIT END-WORD

DEFINE-WORD CR ( -- ) NEWLINE EMIT END-WORD

define-word-created quux-test
100 0 set-fill-data

DEFINE-WORD OVER ( x1 x2 -- x1 x2 x1 ) 1 LIT PICK END-WORD

DEFINE-WORD NIP ( x1 x2 -- x2 ) SWAP DROP END-WORD

DEFINE-WORD TUCK ( x1 x2 -- x2 x1 x2 ) SWAP OVER END-WORD

DEFINE-WORD 2DUP ( x1 x2 -- x1 x2 x1 x2 ) OVER OVER END-WORD

DEFINE-WORD 2DROP ( x1 x2 -- ) DROP DROP END-WORD

DEFINE-WORD <= ( n1 n2 -- flag ) 2DUP = ROT ROT < OR END-WORD

DEFINE-WORD >= ( n1 n2 -- flag ) 2DUP = ROT ROT > OR END-WORD

DEFINE-WORD ?DUP ( 0 | x -- 0 | x x ) DUP 0 LIT <> +IF DUP +THEN END-WORD

DEFINE-WORD-CREATED BASE
10 SET-CELL-DATA

NOT-VM 65 CONSTANT MAX-FORMAT-DIGIT-COUNT VM

DEFINE-WORD-CREATED FORMAT-DIGIT-BUFFER
MAX-FORMAT-DIGIT-COUNT 0 SET-FILL-DATA

DEFINE-WORD COMPLETE-FORMAT-DIGIT-BUFFER ( c-addr -- c-addr u )
  FORMAT-DIGIT-BUFFER MAX-FORMAT-DIGIT-COUNT LIT + OVER -
END-WORD

DEFINE-WORD ADD-CHAR ( c c-addr -- c-addr ) 1 LIT - TUCK C! END-WORD

DEFINE-WORD (FORMAT-DECIMAL) ( n -- c-addr )
  FORMAT-DIGIT-BUFFER MAX-FORMAT-DIGIT-COUNT LIT + +BEGIN
    OVER 0 LIT U>
  +WHILE
    OVER 10 LIT UMOD CHAR 0 LIT + SWAP ADD-CHAR SWAP 10 LIT U/ SWAP
  +REPEAT
  NIP
END-WORD

DEFINE-WORD FORMAT-DECIMAL ( n -- c-addr u )
  DUP 0 LIT < +IF
    NEGATE (FORMAT-DECIMAL) CHAR - LIT SWAP ADD-CHAR
    COMPLETE-FORMAT-DIGIT-BUFFER
  +ELSE
    (FORMAT-DECIMAL) COMPLETE-FORMAT-DIGIT-BUFFER
  +THEN
END-WORD

DEFiNE-WORD FORMAT-UNSIGNED ( n -- c-addr u )
  FORMAT-DIGIT-BUFFER MAX-FORMAT-DIGIT-COUNT LIT + +BEGIN
    OVER 0 LIT U>
  +WHILE
    OVER BASE @ UMOD DUP 10 LIT U< +IF
      CHAR 0 LIT +
    +ELSE
      CHAR A LIT + 10 LIT -
    +THEN
    SWAP ADD-CHAR SWAP BASE @ U/ SWAP
  +REPEAT
  NIP COMPLETE-FORMAT-DIGIT-BUFFER
END-WORD

DEFINE-WORD FORMAT-NUMBER ( n -- c-addr u )
  DUP 0 LIT = +IF
    DROP CHAR 0 LIT FORMAT-DIGIT-BUFFER C! FORMAT-DIGIT-BUFFER 1 LIT
  +ELSE BASE @ 10 LIT = +IF
    FORMAT-DECIMAL
  +ELSE BASE @ DUP 2 LIT >= SWAP 36 LIT <= AND +IF
    FORMAT-UNSIGNED
  +ELSE
    DROP FORMAT-DIGIT-BUFFER 0 LIT
  +THEN +THEN +THEN
END-WORD

DEFINE-WORD FORMAT-UNSIGNED ( n -- c-addr u )
  DUP 0 LIT = +IF
    DROP CHAR 0 LIT FORMAT-DIGIT-BUFFER C! FORMAT-DIGIT-BUFFER 1 LIT
  +ELSE BASE @ DUP 2 LIT >= SWAP 36 LIT <= AND +IF
    FORMAT-UNSIGNED
  +ELSE
    DROP FORMAT-DIGIT-BUFFER 0 LIT
  +THEN +THEN
END-WORD

DEFINE-WORD (.) ( n -- ) FORMAT-NUMBER TYPE END-WORD

DEFINE-WORD (U.) ( u -- ) FORMAT-UNSIGNED TYPE END-WORD

DEFINE-WORD . ( n -- ) (.) SPACE END-WORD

DEFINE-WORD U. ( u -- ) (U.) SPACE END-WORD

DEFINE-WORD-CREATED SBASE
0 SET-CELL-DATA

DEFINE-WORD-CREATED RBASE
0 SET-CELL-DATA

DEFINE-WORD DEPTH ( -- u ) SBASE @ SP@ - CELL-SIZE / 1 LIT - END-WORD

DEFINE-WORD .S ( -- )
  DEPTH 0 LIT >= +IF
    CHAR [ LIT EMIT SPACE DEPTH +BEGIN DUP 0 LIT U> +WHILE
      DUP PICK . 1 LIT -
    +REPEAT
    DROP
    CHAR ] LIT EMIT
  +ELSE
    CHAR [ LIT EMIT CHAR - LIT EMIT CHAR - LIT EMIT CHAR ] LIT EMIT
  +THEN
END-WORD

NOT-VM 256 CONSTANT MAX-SAVED-EXCEPTION-LEN VM

DEFINE-WORD-CREATED SAVED-EXCEPTION-NAME
MAX-SAVED-EXCEPTION-LEN 0 SET-FILL-DATA

DEFINE-WORD-CREATED SAVED-EXCEPTION-LEN
0 SET-CELL-DATA

DEFINE-WORD CMOVE ( c-addr1 c-addr2 bytes -- )
  +BEGIN DUP 0 LIT U> +WHILE
    2 LIT PICK C@ 2 LIT PICK C! 1 LIT - ROT 1 LIT + ROT 1 LIT + ROT
  +REPEAT
  2DROP DROP
END-WORD

DEFINE-WORD SAVE-EXCEPTION-NAME ( c-addr bytes -- )
  DUP MAX-SAVED-EXCEPTION-LEN LIT U> +IF DROP MAX-SAVED-EXCEPTION-LEN LIT +THEN
  DUP SAVED-EXCEPTION-LEN ! SAVED-EXCEPTION-NAME SWAP CMOVE
END-WORD

DEFINE-WORD SAVED-EXCEPTION ( -- c-addry bytes)
  SAVED-EXCEPTION-NAME SAVED-EXCEPTION-LEN @
END-WORD

DEFINE-WORD , ( x -- ) HERE ! CELL-SIZE ALLOT END-WORD

DEFINE-WORD C, ( c -- ) HERE C! 1 LIT ALLOT END-WORD

DEFINE-WORD H, ( c -- ) HERE H! 2 LIT ALLOT END-WORD

DEFINE-WORD W, ( c -- ) HERE W! 4 LIT ALLOT END-WORD

DEFINE-WORD COMPILE, ( token -- )
  NOT-VM TARGET-TOKEN @ TOKEN-8-16 = VM LIT +IF
    DUP $80 LIT U< +IF
      C,
    +ELSE
      DUP $7F LIT AND $80 LIT OR C, 7 LIT RSHIFT
      $FF LIT AND C,
    +THEN
  +ELSE
    NOT-VM TARGET-TOKEN @ TOKEN-16 = VM LIT +IF
      $FFFF LIT AND H,
    +ELSE
      NOT-VM TARGET-TOKEN @ TOKEN-16-32 = VM LIT +IF
        DUP $8000 LIT U< +IF
          H,
        +ELSE
          DUP $7FFF LIT AND $8000 LIT OR H, 15 LIT RSHIFT
          $FFFF LIT AND H,
	+THEN
      +ELSE ( TARGET-TOKEN @ TOKEN-32 NOT-VM = VM )
        $FFFFFFFF LIT AND W,
      +THEN
    +THEN
  +THEN
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY IF ( f -- ) ( Compile-time: -- fore-ref )
  &0BRANCH COMPILE, HERE 0 LIT ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ELSE ( -- )
  ( Compile-time: fore-ref -- fore-ref )
  &BRANCH COMPILE, HERE 0 LIT , HERE ROT !
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY THEN ( -- ) ( Compile-time: fore-ref -- )
  HERE SWAP !
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY BEGIN ( -- ) ( Compile-time: -- back-ref )
  HERE
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY AGAIN ( -- ) ( Compile-time: back-ref -- )
  &BRANCH COMPILE, ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY UNTIL ( -- ) ( Compile-time: back-ref -- )
  &0BRANCH COMPILE, ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY WHILE ( -- ) ( Compile_time: -- fore-ref )
  &0BRANCH COMPILE, HERE 0 LIT ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY REPEAT ( -- )
  ( Compile-time: back-ref fore-ref -- )
  SWAP &BRANCH COMPILE, , HERE SWAP !
END-WORD

DEFINE-WORD-CREATED HANDLER
0 SET-CELL-DATA

DEFINE-WORD TRY ( xt -- exception | 0 )
  SP@ >R HANDLER @ >R RP@ HANDLER ! EXECUTE R> HANDLER ! R> DROP 0
END-WORD

DEFINE-WORD ?RAISE ( xt | 0 -- xt | 0 )
  ?DUP +IF HANDLER @ RP! R> HANDLER ! R> SWAP >R SP! DROP R> +THEN
END-WORD

DEFINE-WORD ??RAISE ( cond xt|0 -- xt|... )
  SWAP 0 = +IF ?RAISE +ELSE DROP +THEN
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY AVERTS ( Compile-time: "name" -- )
  ( Run-time: asserted -- xt|... )
  &LIT COMPILE, ' , &??RAISE COMPILE,
END-WORD

DEFINE-WORD FORTH-WORDLIST ( -- wid ) 0 LIT END-WORD

NOT-VM 256 CONSTANT MAX-WORDLIST-COUNT VM

DEFINE-WORD-CREATED WORDLIST-ARRAY
NOT-VM TARGET-CELL-SIZE MAX-WORDLIST-COUNT * VM 0 SET-FILL-DATA

DEFINE-WORD-CREATED WORDLIST-COUNT
1 SET-CELL-DATA

DEFINE-WORD HANDLE-MAX-WORDLISTS ( -- )
  SPACE S" max wordlists reached" +DATA TYPE CR
END-WORD

DEFINE-WORD HANDLE-OUT-OF-RANGE-WORDLIST ( -- )
  SPACE S" out of range wordlist id" +DATA TYPE CR
END-WORD

DEFINE-WORD WORDLIST ( -- wid )
  WORDLIST-COUNT @ MAX-WORDLIST-COUNT LIT < +IF
    0 LIT WORDLIST-ARRAY WORDLIST-COUNT @ CELLS + !
    WORDLIST-COUNT @ DUP 1 LIT + WORDLIST-COUNT !
  +ELSE
    &HANDLE-MAX-WORDLISTS ?RAISE
  +THEN
END-WORD

DEFINE-WORD WORDLIST>FIRST ( wid -- xt )
  DUP WORDLIST-COUNT @ < +IF
    CELLS WORDLIST-ARRAY + @
  +ELSE
    &HANDLE-OUT-OF-RANGE-WORDLIST ?RAISE
  +THEN
END-WORD

DEFINE-WORD FIRST>WORDLIST ( xt wid -- )
  DUP WORDLIST-COUNT @ < +IF
    CELLS WORDLIST-ARRAY + !
  +ELSE
    &HANDLE-OUT-OF-RANGE-WORDLIST ?RAISE
  +THEN
END-WORD

DEFINE-WORD-CREATED CURRENT-WORDLIST
0 SET-CELL-DATA

DEFINE-WORD SET-CURRENT ( wid -- ) CURRENT-WORDLIST ! END-WORD

DEFINE-WORD GET-CURRENT ( -- wid ) CURRENT-WORDLIST @ END-WORD

DEFINE-WORD UPCASE-CHAR ( c -- c )
  DUP CHAR a LIT >= OVER CHAR z LIT <= AND +IF CHAR a LIT - CHAR A LIT + +THEN
END-WORD

DEFINE-WORD EQUAL-CASE-CHARS ( c-addr1 c-addr2 bytes -- matches )
  +BEGIN DUP 0 LIT > +WHILE
    2 LIT PICK C@ UPCASE-CHAR 2 LIT PICK C@ UPCASE-CHAR <> +IF
      2DROP DROP +FALSE EXIT
    +THEN
    1 LIT - ROT 1 LIT + ROT 1 LIT + ROT
  +REPEAT
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
    DUP WORD>FLAGS HIDDEN-FLAG LIT AND 0 LIT = +IF
      2 LIT PICK 2 LIT PICK 2 LIT PICK WORD>NAME EQUAL-NAME +IF
        ROT ROT 2DROP +TRUE EXIT
      +THEN
    +THEN
    WORD>NEXT
  +REPEAT
  2DROP DROP 0 LIT +FALSE
END-WORD

NOT-VM 128 CONSTANT MAX-WORDLIST-ORDER-COUNT VM

DEFINE-WORD-CREATED WORDLIST-ORDER-ARRAY
NOT-VM TARGET-CELL-SIZE MAX-WORDLIST-ORDER-COUNT * 0 SET-FILL-DATA VM

DEFINE-WORD-CREATED WORDLIST-ORDER-COUNT
1 SET-CELL-DATA

DEFINE-WORD HANDLE-MAX-WORDLIST-ORDER-COUNT ( -- )
  SPACE S" max wordlist order count reached" +DATA TYPE CR
END-WORD

DEFINE-WORD SET-ORDER ( widn ... wid1 count -- )
  DUP MAX-WORDLIST-ORDER-COUNT LIT <= +IF
    DUP WORDLIST-ORDER-COUNT !
    0 LIT +BEGIN 2DUP > +WHILE
      ROT OVER CELLS WORDLIST-ORDER-ARRAY + ! 1 LIT +
    +REPEAT
    2DROP
  +ELSE
    &HANDLE-MAX-WORDLIST-ORDER-COUNT ?RAISE
  +THEN
END-WORD

DEFINE-WORD GET-ORDER ( -- widn ... wid1 count )
  WORDLIST-ORDER-COUNT @ +BEGIN DUP 0 LIT > +WHILE
    1 LIT - DUP CELLS WORDLIST-ORDER-ARRAY + @ SWAP
  +REPEAT
  DROP WORDLIST-ORDER-COUNT @
END-WORD

DEFINE-WORD SEARCH-WORDLISTS ( c-addr bytes -- xt found )
  0 LIT +BEGIN DUP WORDLIST-ORDER-COUNT @ < +WHILE
    DUP CELL-SIZE * WORDLIST-ORDER-ARRAY + @ 3 LIT PICK 3 LIT PICK ROT
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

DEFINE-WORD-CREATED TIB#
0 SET-CELL-DATA

DEFINE-WORD HANDLE-COMPILE-ONLY-ERROR ( -- )
  SPACE SAVED-EXCEPTION TYPE S" : compile-only word" +DATA TYPE CR
END-WORD

DEFINE-WORD HANDLE-PARSE-ERROR ( -- )
  SPACE SAVED-EXCEPTION TYPE S" : parse error" +DATA TYPE CR
END-WORD

DEFINE-WORD HANDLE-NO-PARSE-FOUND ( -- )
  SPACE S" input expected" +DATA TYPE CR
END-WORD

DEFINE-WORD HANDLE-NO-WORD-FOUND ( -- )
  SPACE SAVED-EXCEPTION TYPE S" : word not found" +DATA TYPE CR
END-WORD

DEFINE-WORD HANDLE-NO-LATESTXT ( -- )
  SPACE S" no latestxt exists" +DATA TYPE CR
END-WORD

DEFINE-WORD INPUT-LEFT? ( -- f ) >IN @ INPUT# @ < END-WORD

DEFINE-WORD INPUT-NOT-WS? ( -- f )
  INPUT @ >IN @ + C@ DUP BL <> OVER NEWLINE <> AND SWAP TAB <> AND  
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
    >IN @ ADVANCE-TO-NAME-END INPUT @ OVER + >IN @ ROT -
  +ELSE
    0 LIT 0 LIT
  +THEN
END-WORD

DEFINE-WORD INPUT-CLOSE-PAREN? ( -- f ) INPUT @ >IN @ + C@ CHAR ) LIT = END-WORD

NON-DEFINE-WORD-IMMEDIATE ( ( -- )
  +BEGIN
    INPUT-LEFT? +IF INPUT-CLOSE-PAREN? ADVANCE->IN +ELSE +TRUE +THEN
  +UNTIL
END-WORD

DEFINE-WORD INPUT-NEWLINE? ( -- f ) INPUT @ >IN @ + C@ NEWLINE = END-WORD

NON-DEFINE-WORD-IMMEDIATE \ ( -- )
  +BEGIN
    INPUT-LEFT? +IF INPUT-NEWLINE? ADVANCE->IN +ELSE +TRUE +THEN
  +UNTIL
END-WORD

DEFINE-WORD INPUT-QUOTE? ( -- f ) INPUT @ >IN @ + C@ CHAR " LIT = END-WORD

DEFINE-WORD PARSE-STRING ( "ccc<quote>" -- c-addr bytes )
  ADVANCE-TO-NAME-START +IF
    INPUT @ >IN @ + 0 LIT +BEGIN
      INPUT-LEFT? +IF
        INPUT-QUOTE? +IF +TRUE +ELSE 1 LIT + +FALSE +THEN ADVANCE->IN
      +ELSE
        +TRUE
      +THEN
    +UNTIL
  +ELSE
    &HANDLE-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

NON-DEFINE-WORD-IMMEDIATE-COMPILE-ONLY S" ( Compile-time "ccc<quote>" -- )
  ( Runtime: -- c-addr bytes )
  PARSE-STRING
  &(DATA) COMPILE, DUP , SWAP HERE 2 LIT PICK CMOVE DUP ALLOT &LIT COMPILE, ,
END-WORD

NON-DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ." ( Compile-time "ccc<quote>" -- )
  ( Runtime: -- c-addr bytes )
  PARSE-STRING
  &(DATA) COMPILE, DUP , SWAP HERE 2 LIT PICK CMOVE DUP ALLOT &LIT COMPILE, ,
  &TYPE COMPILE,
END-WORD

DEFINE-WORD INPUT-CLOSE-PAREN? ( -- f ) INPUT @ >IN @ + C@ CHAR ) LIT = END-WORD

DEFINE-WORD PARSE-PAREN-STRING ( "ccc<quote>" -- c-addr bytes )
  ADVANCE-TO-NAME-START +IF
    INPUT @ >IN @ + 0 LIT +BEGIN
      INPUT-LEFT? +IF
        INPUT-CLOSE-PAREN? +IF +TRUE +ELSE 1 LIT + +FALSE +THEN ADVANCE->IN
      +ELSE
        +TRUE
      +THEN
    +UNTIL
  +ELSE
    &HANDLE-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

NON-DEFINE-WORD-IMMEDIATE .( ( Compile-time "ccc<close-paren>" -- )
  ( Runtime: -- c-addr bytes )
  PARSE-PAREN-STRING TYPE
END-WORD

NON-DEFINE-WORD CHAR ( -- c )
  PARSE-NAME 0 LIT <> +IF
    C@
  +ELSE
    DROP &HANDLE-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY [CHAR] ( Run-time: -- c )
  ^CHAR TOKEN, &LIT COMPILE, ,
END-WORD

DEFINE-WORD ' ( "name" -- xt )
  PARSE-NAME DUP 0 LIT <> +IF
    2DUP SEARCH-WORDLISTS +IF
      ROT ROT 2DROP
    +ELSE
      DROP SAVE-EXCEPTION-NAME &HANDLE-NO-WORD-FOUND ?RAISE
    +THEN
  +ELSE
    2DROP &HANDLE-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY LITERAL ( Compile-time: x -- )
  ( Run-time: -- x )
  &LIT COMPILE, ,
END-WORD

DEFINE-WORD-IMMEDIATE ['] ( Compile-time: "name" -- ) ( Run-time: -- xt )
  &LIT COMPILE, ' ,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY COMPILE ( Compile-time: "name" -- )
  &LIT COMPILE, ' , &COMPILE, COMPILE,
END-WORD

DEFINE-WORD-IMMEDIATE-COMPILE-ONLY [COMPILE] ( Compile-time: "name" -- )
  ' COMPILE,
END-WORD

DEFINE-WORD IMMEDIATE? ( xt -- flag )
  WORD>FLAGS IMMEDIATE-FLAG LIT AND 0 LIT <>
END-WORD

DEFINE-WORD-CREATED STATE
0 SET-CELL-DATA

DEFINE-WORD-CREATED LATESTXT-VALUE
0 SET-CELL-DATA

DEFINE-WORD ALLOCATE-STRING ( c-addr bytes -- c-addr bytes )
  HERE OVER ALLOT ROT 2 LIT PICK 2 LIT PICK SWAP CMOVE SWAP
END-WORD

DEFINE-WORD : ( -- )
  PARSE-NAME DUP 0 LIT <> +IF
    ALLOCATE-STRING +TRUE STATE ! HERE NEW-COLON DUP 3 LIT ROLL 3 LIT ROLL ROT
    NAME>WORD HIDDEN-FLAG LIT OVER FLAGS>WORD DUP LATESTXT-VALUE !
    GET-CURRENT WORDLIST>FIRST OVER NEXT>WORD
    GET-CURRENT FIRST>WORDLIST
  +ELSE
    2DROP &HANDLE-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

DEFINE-WORD :NONAME ( -- xt )
  TRUE STATE ! HERE NEW-COLON DUP LATESTXT-VALUE !
END-WORD

DEFINE-WORD CREATE ( -- )
  PARSE-NAME DUP 0 LIT <> +IF
    ALLOCATE-STRING HERE NEW-CREATE DUP 3 LIT ROLL 3 LIT ROLL ROT
    NAME>WORD DUP LATESTXT-VALUE !
    GET-CURRENT WORDLIST>FIRST OVER NEXT>WORD
    GET-CURRENT FIRST>WORDLIST
  +ELSE
    2DROP &HANDLE-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

DEFINE-WORD LATESTXT ( -- xt ) LATESTXT-VALUE @ END-WORD

DEFINE-WORD IMMEDIATE ( -- )
  LATESTXT WORD>FLAGS IMMEDIATE-FLAG LIT OR LATESTXT FLAGS>WORD
END-WORD

DEFINE-WORD COMPILE-ONLY ( -- )
  LATESTXT WORD>FLAGS COMPILE-ONLY-FLAG LIT OR LATESTXT FLAGS>WORD
END-WORD

NON-DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ; ( -- )
  LATESTXT +IF
    &EXIT COMPILE, &END COMPILE,
    LATESTXT WORD>FLAGS HIDDEN-FLAG LIT NOT AND LATESTXT FLAGS>WORD
    LATESTXT FINISH
    +FALSE STATE !
  +ELSE
    &HANDLE-NO-LATESTXT ?RAISE
  +THEN
END-WORD

DEFINE-WORD DOES> ( -- )
  LATESTXT ?DUP +IF SET-DOES> +ELSE &HANDLE-NO-LATESTXT ?RAISE +THEN
END-WORD

DEFINE-WORD CONSTANT ( x -- ) CREATE , DOES> @ END-WORD

DEFINE-WORD VARIABLE ( -- ) CREATE 0 LIT , END-WORD

DEFINE-WORD-CREATED BASE
10 SET-CELL-DATA

DEFINE-WORD ADVANCE-STRING ( c-addr1 bytes1 -- c-addr2 bytes2 )
  1 LIT - SWAP 1 LIT + SWAP
END-WORD

DEFINE-WORD PARSE-BASE ( c-addr1 bytes1 -- c-addr2 bytes2 base )
  DUP 0 LIT > +IF
    OVER C@ DUP CHAR $ LIT = +IF DROP ADVANCE-STRING 16 LIT
    +ELSE DUP CHAR # LIT = +IF DROP ADVANCE-STRING 10 LIT
    +ELSE DUP CHAR / LIT = +IF DROP ADVANCE-STRING 8 LIT
    +ELSE CHAR % LIT = +IF ADVANCE-STRING 2 LIT
    +ELSE BASE @
    +THEN +THEN +THEN +THEN
  +ELSE
    BASE @
  +THEN
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
    UPCASE-CHAR DUP CHAR A LIT >= OVER CHAR Z LIT <= AND +IF
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
    +THEN
  +REPEAT
  SWAP DROP SWAP DROP SWAP DROP +TRUE
END-WORD

DEFINE-WORD PARSE-NUMBER ( c-addr bytes -- n matches )
  PARSE-BASE ROT ROT DUP 0 LIT U> +IF
    OVER C@ CHAR - LIT = +IF
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

DEFINE-WORD COMPILE-WORD ( c-addr bytes xt -- )
  ROT ROT 2DROP DUP WORD>FLAGS IMMEDIATE-FLAG LIT AND +IF
    EXECUTE
  +ELSE
    COMPILE,
  +THEN
END-WORD

DEFINE-WORD INTERPRET-WORD ( c-addr bytes xt -- )
  DUP WORD>FLAGS COMPILE-ONLY-FLAG LIT AND +IF
    DROP SAVE-EXCEPTION-NAME &HANDLE-COMPILE-ONLY-ERROR ?RAISE
  +ELSE
    ROT ROT 2DROP EXECUTE
  +THEN
END-WORD

DEFINE-WORD HANDLE-NUMBER ( c-addr bytes -- )
  2DUP PARSE-NUMBER +IF
    ROT ROT 2DROP
    STATE @ +IF
      &LIT COMPILE, ,
    +THEN
  +ELSE
    DROP SAVE-EXCEPTION-NAME &HANDLE-PARSE-ERROR ?RAISE
  +THEN
END-WORD

DEFINE-WORD HANDLE-DATA-STACK-UNDERFLOW ( -- )
  SPACE S" data stack underflow" +DATA TYPE CR
END-WORD

DEFINE-WORD HANDLE-RETURN-STACK-UNDERFLOW ( -- )
  SPACE S" return stack underflow" +DATA TYPE CR
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
	SP@ SBASE @ > +IF &HANDLE-DATA-STACK-UNDERFLOW ?RAISE +THEN
	RP@ RBASE @ > +IF &HANDLE-RETURN-STACK-UNDERFLOW ?RAISE +THEN
	+FALSE
      +ELSE
        DROP HANDLE-NUMBER +FALSE
      +THEN
    +ELSE
      DROP +TRUE
    +THEN
  +UNTIL
END-WORD

DEFINE-WORD EVALUATE ( c-addr bytes -- )
  INPUT @ >R INPUT# @ >R >IN @ >R
  0 LIT >IN ! INPUT# ! INPUT !
  &INTERPRET TRY R> >IN ! R> INPUT# ! R> INPUT ! ?DUP +IF ?RAISE +THEN
END-WORD

DEFINE-WORD REFILL-TERMINAL ( -- )
  TIB TIB-SIZE LIT ACCEPT DUP TIB# ! INPUT# ! TIB INPUT ! 0 LIT >IN !
END-WORD

DEFINE-WORD-CREATED 'REFILL
0 SET-CELL-DATA

DEFINE-WORD REFILL ( -- ) 'REFILL @ ?DUP +IF EXECUTE +THEN END-WORD

DEFINE-WORD-CREATED 'PAUSE

DEFINE-WORD PAUSE ( -- ) 'PAUSE @ ?DUP +IF EXECUTE +THEN END-WORD

DEFINE-WORD ABORT-EXCEPTION ( -- ) SPACE S" aborted" +DATA TYPE CR END-WORD

DEFINE-WORD QUIT-EXCEPTION ( -- ) SPACE S" quit" +DATA TYPE CR END-WORD

DEFINE-WORD-CREATED STORAGE
0 SET-CELL-DATA

DEFINE-WORD-CREATED STORAGE#
0 SET-CELL-DATA

DEFINE-WORD INTERPRET-STORAGE ( -- )
  INPUT @ >R INPUT# @ >R >IN @ >R
  0 LIT >IN ! STORAGE# @ INPUT# ! STORAGE @ INPUT !
  &INTERPRET TRY R> >IN ! R> INPUT# ! R> INPUT ! ?DUP +IF
    DUP &QUIT-EXCEPTION = +IF
      EXECUTE +FALSE STATE !
    +ELSE
      EXECUTE SBASE @ SP! +FALSE STATE !
    +THEN
  +THEN
END-WORD

DEFINE-WORD OUTER ( -- )
  RBASE @ RP!
  +BEGIN
    REFILL
    &INTERPRET TRY DUP 0 LIT = +IF
      DROP STATE @ 0 LIT = INPUT @ TIB = AND +IF
        SPACE S" ok" +DATA TYPE CR
      +THEN
    +ELSE
      DUP &QUIT-EXCEPTION = +IF
        EXECUTE RBASE @ RP! +FALSE STATE !
      +ELSE
        EXECUTE SBASE @ SP! RBASE @ RP! +FALSE STATE !
      +THEN
    +THEN
  +AGAIN
END-WORD

DEFINE-WORD ABORT &ABORT-EXCEPTION ?RAISE END-WORD

DEFINE-WORD QUIT &QUIT-EXCEPTION ?RAISE END-WORD

DEFINE-WORD MARKER ( "name" -- )
  LATESTXT HERE CREATE , , WORDLIST-COUNT @ ,
  WORDLIST-ARRAY WORDLIST-COUNT @ +BEGIN DUP 0 LIT > +WHILE
    SWAP DUP @ DUP LATESTXT = +IF WORD>NEXT +THEN , CELL+ SWAP 1 LIT -
  +REPEAT
  2DROP WORDLIST-ORDER-COUNT @ ,
  WORDLIST-ORDER-ARRAY WORDLIST-ORDER-COUNT @ +BEGIN DUP 0 LIT > +WHILE
    SWAP DUP @ , CELL+ SWAP 1 LIT -
  +REPEAT
  2DROP GET-CURRENT ,
  DOES>
  DUP @ HERE! CELL+ DUP @ DUP LATESTXT-VALUE ! 1 LIT + SET-WORD-COUNT CELL+
  DUP @ DUP WORDLIST-COUNT !
  SWAP CELL+ SWAP WORDLIST-ARRAY SWAP +BEGIN DUP 0 LIT > +WHILE
    SWAP ROT DUP @ ROT TUCK ! CELL+ SWAP CELL+ SWAP ROT 1 LIT -
  +REPEAT
  2DROP DUP @ DUP WORDLIST-ORDER-COUNT !
  SWAP CELL+ SWAP WORDLIST-ORDER-ARRAY SWAP +BEGIN DUP 0 LIT > +WHILE
    SWAP ROT DUP @ ROT TUCK ! CELL+ SWAP CELL+ SWAP ROT 1 LIT -
  +REPEAT
  2DROP @ SET-CURRENT
END-WORD

DEFINE-WORD BOOT ( -- )
  SP@ SBASE ! RP@ RBASE ! TIB INPUT ! 0 LIT TIB# ! 0 LIT INPUT# !
  &BOOT WORDLIST-ARRAY 0 LIT CELLS + !
  &REFILL-TERMINAL 'REFILL ! INTERPRET-STORAGE OUTER
END-WORD

S" src/forth/startup.fs" ADD-SOURCE-TO-STORAGE
S" src/forth/lambda.fs" ADD-SOURCE-TO-STORAGE

S" images/cell_64_token_16_32.image" WRITE-ASM-TO-FILE
