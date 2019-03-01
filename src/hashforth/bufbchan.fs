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

BEGIN-STRUCTURE BUFBCHAN-SIZE
  FIELD: BUFBCHAN-RECV-COND
  FIELD: BUFBCHAN-SEND-COND
  FIELD: BUFBCHAN-QUEUE
  FIELD: BUFBCHAN-QUEUE-COUNT
  FIELD: BUFBCHAN-QUEUE-SIZE
  FIELD: BUFBCHAN-ENTRY-SIZE
  FIELD: BUFBCHAN-ENQUEUE-INDEX
  FIELD: BUFBCHAN-DEQUEUE-INDEX
END-STRUCTURE

\ Print out the internal values of a bounded channel.
: BUFBCHAN. ( chan -- )
  CR ." BUFBCHAN-RECV-COND: " DUP BUFBCHAN-RECV-COND @ .
  CR ." BUFBCHAN-SEND-COND: " DUP BUFBCHAN-SEND-COND @ .
  CR ." BUFBCHAN-QUEUE: " DUP BUFBCHAN-QUEUE @ .
  CR ." BUFBCHAN-QUEUE-COUNT: " DUP BUFBCHAN-QUEUE-COUNT @ .
  CR ." BUFBCHAN-QUEUE-SIZE: " DUP BUFBCHAN-QUEUE-SIZE @ .
  CR ." BUFBCHAN-ENTRY-SIZE: " DUP BUFBCHAN-ENTRY-SIZE @ .
  CR ." BUFBCHAN-ENQUEUE-INDEX: " DUP BUFBCHAN-ENQUEUE-INDEX @ .
  CR ." BUFBCHAN-DEQUEUE-INDEX: " BUFBCHAN-DEQUEUE-INDEX @ . CR ;

\ Create a new bounded channel with the specified queue size, entry size, and
\ condition variable queue size.
: NEW-BUFBCHAN ( queue-size entry-size cond-size -- chan )
  HERE BUFBCHAN-SIZE ALLOT
  3 PICK OVER BUFBCHAN-QUEUE-SIZE !
  0 OVER BUFBCHAN-QUEUE-COUNT !
  0 OVER BUFBCHAN-ENQUEUE-INDEX !
  0 OVER BUFBCHAN-DEQUEUE-INDEX !
  HERE 4 ROLL 4 PICK * ALLOT OVER BUFBCHAN-QUEUE !
  OVER NEW-COND OVER BUFBCHAN-RECV-COND !
  SWAP NEW-COND OVER BUFBCHAN-SEND-COND !
  TUCK BUFBCHAN-ENTRY-SIZE ! ;

\ Internal - dequeue a packet from a bounded channel; note that this does not
\ have any safeties for preventing dequeueing a value from an already empty
\ bounded channel, nor does this wake up any tasks waiting to send on the
\ bounded channel.
: DEQUEUE-BUFBCHAN ( addr chan -- )
  DUP BUFBCHAN-QUEUE @ OVER BUFBCHAN-DEQUEUE-INDEX @
  2 PICK BUFBCHAN-ENTRY-SIZE @ * + ROT 2 PICK BUFBCHAN-ENTRY-SIZE @ CMOVE
  DUP BUFBCHAN-QUEUE-COUNT @ 1- OVER BUFBCHAN-QUEUE-COUNT !
  DUP BUFBCHAN-DEQUEUE-INDEX @ 1+ OVER BUFBCHAN-QUEUE-SIZE @ MOD
  SWAP BUFBCHAN-DEQUEUE-INDEX ! ;

\ Internal - enqueue a packet onto a bounded channel; note that this does not
\ have any safeties for preventing enqueuing a value onto an already full
\ bounded channel, nor does this wake up any tasks waiting to receiving on the
\ bounded channel.
: ENQUEUE-BUFBCHAN ( addr chan -- )
  TUCK BUFBCHAN-QUEUE @ 2 PICK BUFBCHAN-ENQUEUE-INDEX @
  3 PICK BUFBCHAN-ENTRY-SIZE @ * + 2 PICK BUFBCHAN-ENTRY-SIZE @ CMOVE
  DUP BUFBCHAN-QUEUE-COUNT @ 1+ OVER BUFBCHAN-QUEUE-COUNT !
  DUP BUFBCHAN-ENQUEUE-INDEX @ 1+ OVER BUFBCHAN-QUEUE-SIZE @ MOD
  SWAP BUFBCHAN-ENQUEUE-INDEX ! ;

\ Internal - peek a value from a bounded channel; note that this does not have
\ any safeties for preventing peeking a value from an empty bounded channel.
: DO-PEEK-BUFBCHAN ( addr chan -- )
  DUP BUFBCHAN-QUEUE @ OVER BUFBCHAN-DEQUEUE-INDEX @
  2 PICK BUFBCHAN-ENTRY-SIZE @ * + ROT ROT BUFBCHAN-ENTRY-SIZE @ CMOVE ;

\ Send a packet on a bounded channel, waking up one task waiting to receive a
\ packet from the bounded channel, and waiting for a value to be read from the
\ bounded channel if it is already full.
: SEND-BUFBCHAN ( addr chan -- )
  BEGIN DUP BUFBCHAN-QUEUE-COUNT @ OVER BUFBCHAN-QUEUE-SIZE @ >= WHILE
    DUP BUFBCHAN-SEND-COND @ WAIT-COND
  REPEAT
  TUCK ENQUEUE-BUFBCHAN BUFBCHAN-RECV-COND @ SIGNAL-COND ;

\ Attempt to send a packet on a bounded channel, waking up one task waiting to
\ receive a packet from the bounded channel, and returning FALSE if the bounded
\ channel is already full.
: TRY-SEND-BUFBCHAN ( addr chan -- success )
  DUP BUFBCHAN-QUEUE-COUNT @ OVER BUFBCHAN-QUEUE-SIZE @ < IF
    TUCK ENQUEUE-BUFBCHAN BUFBCHAN-RECV-COND @ SIGNAL-COND TRUE
  ELSE
    2DROP FALSE
  THEN ;

\ Receive a packet from a bounded channel, waking up one task waiting to send
\ a packet on the bounded channel, and waiting for a task to send a packet on
\ the bounded channel if it is empty.
: RECV-BUFBCHAN ( addr chan -- )
  BEGIN DUP BUFBCHAN-QUEUE-COUNT @ 0= WHILE
    DUP BUFBCHAN-RECV-COND @ WAIT-COND
  REPEAT
  TUCK DEQUEUE-BUFBCHAN BUFBCHAN-SEND-COND @ SIGNAL-COND ;

\ Receive a packet from a bounded channel without dequeueing any packets,
\ waiting for a task to send a packet on the bounded channel if it is empty.
: PEEK-BUFBCHAN ( addr chan -- )
  BEGIN DUP BUFBCHAN-QUEUE-COUNT @ 0= WHILE
    DUP BUFBCHAN-RECV-COND @ WAIT-COND
  REPEAT
  DO-PEEK-BUFBCHAN ;

\ Attempt to receive a packet from a bounded channel, waking up one task waiting
\ to send a packet on the bounded channel, and returning FALSE if the bounded
\ channel is empty.
: TRY-RECV-BUFBCHAN ( addr chan -- found )
  DUP BUFBCHAN-QUEUE-COUNT @ 0<> IF
    TUCK DEQUEUE-BUFBCHAN BUFBCHAN-SEND-COND @ SIGNAL-COND TRUE
  ELSE
    2DROP FALSE
  THEN ;

\ Attempt to receive a packet from a bounded channel without dequeueing any
\ packets, returning FALSE if the bounded channel is empty.
: TRY-PEEK-BUFBCHAN ( addr chan -- found )
  DUP BUFBCHAN-QUEUE-COUNT @ 0<> IF
    DO-PEEK-BUFBCHAN TRUE
  ELSE
    2DROP 0 FALSE
  THEN ;

\ Get the number of values queued in a bounded channel.
: COUNT-BUFBCHAN ( chan -- u ) BUFBCHAN-QUEUE-COUNT @ ;

\ Get whether a bounded channel is empty.
: EMPTY-BUFBCHAN? ( chan -- empty ) BUFBCHAN-QUEUE-COUNT @ 0 = ;

\ Get whether a bounded channel is full.
: FULL-BUFBCHAN? ( chan -- full )
  DUP BUFBCHAN-QUEUE-COUNT @ SWAP BUFBCHAN-QUEUE-SIZE @ = ;

\ Get bounded channel entry size.
: GET-BUFBCHAN-ENTRY-SIZE ( chan -- u ) BUFBCHAN-ENTRY-SIZE @ ;

BASE ! SET-CURRENT SET-ORDER