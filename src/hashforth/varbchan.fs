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
FORTH-WORDLIST TASK-WORDLIST 2 SET-ORDER
TASK-WORDLIST SET-CURRENT

BEGIN-STRUCTURE VARBCHAN-SIZE
  FIELD: VARBCHAN-BUFBCHAN
  FIELD: VARBCHAN-SEND-MUTEX
  FIELD: VARBCHAN-RECV-MUTEX
END-STRUCTURE

BEGIN-STRUCTURE VARBCHAN-HEADER-SIZE
  FIELD: VARBCHAN-REMAIN-SIZE
END-STRUCTURE

\ Create a new variable-sized bounded channel, with the specified queue size,
\ entry size, condition variable queue size, send mutex queue size, and
\ receive mutex queue size.
: NEW-VARBCHAN
  ( queue-size entry-size cond-size send-mutex-size recv-mutex-size -- chan )
  HERE VARBCHAN-SIZE ALLOT
  TUCK SWAP NEW-MUTEX SWAP VARBCHAN-RECV-MUTEX !
  TUCK SWAP NEW-MUTEX SWAP VARBCHAN-SEND-MUTEX !
  3 ROLL 3 ROLL VARBCHAN-HEADER-SIZE + 3 ROLL
  NEW-BUFBCHAN OVER VARBCHAN-BUFBCHAN ! ;

\ Get the size of the next packet.
: GET-NEXT-VARBCHAN-SIZE ( chan -- bytes )
  HERE SWAP VARBCHAN-BUFBCHAN @ PEEK-BUFBCHAN
  HERE VARBCHAN-REMAIN-SIZE @ ;

\ Get the maximum number of bytes to send per packet.
: GET-VARBCHAN-MAX-DATA-SIZE ( chan -- bytes )
  VARBCHAN-BUFBCHAN @ GET-BUFBCHAN-ENTRY-SIZE VARBCHAN-HEADER-SIZE - ;

\ Get the number of bytes to send per packet.
: GET-VARBCHAN-DATA-SIZE ( bytes1 chan -- bytes2 )
  GET-VARBCHAN-MAX-DATA-SIZE SWAP MIN ;

\ Advance HERE pointer
: ADVANCE-HERE ( chan -- )
  VARBCHAN-BUFBCHAN @ GET-BUFBCHAN-ENTRY-SIZE ALLOT ;

\ Retract HERE pointer
: RETRACT-HERE ( chan -- )
  VARBCHAN-BUFBCHAN @ GET-BUFBCHAN-ENTRY-SIZE NEGATE ALLOT ;

\ Actually send a packet.
: (SEND-VARBCHAN) ( addr bytes chan -- )
  OVER 0 ?DO
    OVER HERE VARBCHAN-REMAIN-SIZE !
    2DUP GET-VARBCHAN-DATA-SIZE >R
    2 PICK HERE VARBCHAN-HEADER-SIZE + R@ CMOVE
    ROT R@ + ROT R> - ROT
    HERE OVER VARBCHAN-BUFBCHAN @ 2 PICK ADVANCE-HERE SEND-BUFBCHAN
    DUP RETRACT-HERE
  DUP GET-VARBCHAN-MAX-DATA-SIZE +LOOP
  DROP 2DROP ;

\ Send a packet.
: SEND-VARBCHAN ( addr bytes chan -- )
  DUP VARBCHAN-SEND-MUTEX @ ['] (SEND-VARBCHAN) WITH-MUTEX ;

\ Actually receive a packet
: (RECV-VARBCHAN) ( addr bytes1 chan -- bytes2 )
  0 >R BEGIN
    OVER 0 > IF
      HERE OVER VARBCHAN-BUFBCHAN @ 2 PICK ADVANCE-HERE RECV-BUFBCHAN
      DUP RETRACT-HERE
      HERE VARBCHAN-HEADER-SIZE + 3 PICK
      HERE VARBCHAN-REMAIN-SIZE @ 3 PICK GET-VARBCHAN-DATA-SIZE
      4 PICK MIN >R R@ CMOVE ROT R@ + ROT R@ - ROT R> R> + >R
      HERE VARBCHAN-REMAIN-SIZE @ OVER GET-VARBCHAN-MAX-DATA-SIZE <=
    ELSE
      HERE OVER VARBCHAN-BUFBCHAN @ RECV-BUFBCHAN
      HERE VARBCHAN-REMAIN-SIZE @ OVER GET-VARBCHAN-MAX-DATA-SIZE <=
    THEN
  UNTIL
  2DROP DROP R> ;

\ Receive a packet.
: RECV-VARBCHAN ( addr bytes1 chan -- bytes2 )
  DUP VARBCHAN-RECV-MUTEX @ ['] (RECV-VARBCHAN) WITH-MUTEX ;

BASE ! SET-CURRENT SET-ORDER