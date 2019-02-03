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

BEGIN-STRUCTURE BCHAN-SIZE
  FIELD: BCHAN-RECV-COND
  FIELD: BCHAN-SEND-COND
  FIELD: BCHAN-QUEUE
  FIELD: BCHAN-QUEUE-COUNT
  FIELD: BCHAN-QUEUE-SIZE
  FIELD: BCHAN-ENQUEUE-INDEX
  FIELD: BCHAN-DEQUEUE-INDEX
END-STRUCTURE

8 CONSTANT BCHAN-QUEUE-DEFAULT-SIZE

\ Print out the internal values of a bounded channel.
: BCHAN. ( chan -- )
  CR ." BCHAN-RECV-COND: " DUP BCHAN-RECV-COND @ .
  CR ." BCHAN-SEND-COND: " DUP BCHAN-SEND-COND @ .
  CR ." BCHAN-QUEUE: " DUP BCHAN-QUEUE @ .
  CR ." BCHAN-QUEUE-COUNT: " DUP BCHAN-QUEUE-COUNT @ .
  CR ." BCHAN-QUEUE-SIZE: " DUP BCHAN-QUEUE-SIZE @ .
  CR ." BCHAN-ENQUEUE-INDEX: " BCHAN-ENQUEUE-INDEX @ .
  CR ." BCHAN-DEQUEUE-INDEX: " BCHAN-DEQUEUE-INDEX @ . CR ;

\ Create a new bounded channel with the specified queue size and condition
\ variable queue size.
: NEW-BCHAN ( queue-size cond-size -- chan )
  SWAP
  HERE BCHAN-SIZE ALLOT
  2DUP BCHAN-QUEUE-SIZE !
  0 OVER BCHAN-QUEUE-COUNT !
  0 OVER BCHAN-ENQUEUE-INDEX !
  0 OVER BCHAN-DEQUEUE-INDEX !
  HERE ROT CELLS ALLOT OVER BCHAN-QUEUE !
  OVER NEW-COND OVER BCHAN-RECV-COND !
  SWAP NEW-COND OVER BCHAN-SEND-COND ! ;

\ Internal - dequeue a value from a bounded channel; note that this does not
\ have any safeties for preventing dequeueing a value from an already empty
\ bounded channel, nor does this wake up any tasks waiting to send on the
\ bounded channel.
: DEQUEUE-BCHAN ( chan -- x )
  DUP BCHAN-QUEUE @ OVER BCHAN-DEQUEUE-INDEX @ CELLS + @
  OVER BCHAN-QUEUE-COUNT @ 1- 2 PICK BCHAN-QUEUE-COUNT !
  OVER BCHAN-DEQUEUE-INDEX @ 1+ 2 PICK BCHAN-QUEUE-SIZE @ MOD
  ROT BCHAN-DEQUEUE-INDEX ! ;

\ Internal - enqueue a value onto a bounded channel; note that this does not
\ have any safeties for preventing enqueuing a value onto an already full
\ bounded channel, nor does this wake up any tasks waiting to receiving on the
\ bounded channel.
: ENQUEUE-BCHAN ( x chan -- )
  TUCK BCHAN-QUEUE @ 2 PICK BCHAN-ENQUEUE-INDEX @ CELLS + !
  DUP BCHAN-QUEUE-COUNT @ 1+ 2 PICK BCHAN-QUEUE-COUNT !
  DUP BCHAN-ENQUEUE-INDEX @ 1+ OVER BCHAN-QUEUE-SIZE @ MOD
  SWAP BCHAN-ENQUEUE-INDEX ! ;

\ Send a value on a bounded channel, waking up one task waiting to receive a
\ value from the bounded channel, and waiting for a value to be read from the
\ bounded channel if it is already full.
: SEND-BCHAN ( x chan -- )
  BEGIN DUP BCHAN-QUEUE-COUNT @ OVER BCHAN-QUEUE-SIZE @ >= WHILE
    DUP BCHAN-SEND-COND @ WAIT-COND
  REPEAT
  SWAP OVER ENQUEUE-BCHAN BCHAN-RECV-COND @ SIGNAL-COND ;

\ Attempt to send a value on a bounded channel, waking up one task waiting to
\ receive a value from the bounded channel, and returning FALSE if the bounded
\ channel is already full.
: TRY-SEND-BCHAN ( x chan -- success )
  DUP BCHAN-QUEUE-COUNT @ OVER BCHAN-QUEUE-SIZE @ < IF
    TUCK ENQUEUE-BCHAN BCHAN-RECV-COND @ SIGNAL-COND TRUE
  ELSE
    2DROP FALSE
  THEN ;

\ Receive a value from a bounded channel, waking up one task waiting to send
\ a value on the bounded channel, and waiting for a task to send a value on
\ the bounded channel if it is empty.
: RECV-BCHAN ( chan -- x )
  BEGIN DUP BCHAN-QUEUE-COUNT @ 0= WHILE
    DUP BCHAN-RECV-COND @ WAIT-COND
  REPEAT
  DUP DEQUEUE-BCHAN SWAP BCHAN-SEND-COND @ SIGNAL-COND ;

\ Receive a value from a bounded channel without dequeueing any values, waiting
\ for a task to send a value on the bounded channel if it is empty.
: PEEK-BCHAN ( chan -- x )
  BEGIN DUP BCHAN-QUEUE-COUNT @ 0= WHILE
    DUP BCHAN-RECV-COND @ WAIT-COND
  REPEAT
  BCHAN-QUEUE @ @ ;

\ Attempt to receive a value from a bounded channel, waking up one task waiting
\ to send a value on the bounded channel, and returning FALSE if the bounded
\ channel is empty.
: TRY-RECV-BCHAN ( chan -- x found )
  DUP BCHAN-QUEUE-COUNT @ 0<> IF
    DUP DEQUEUE-BCHAN SWAP BCHAN-SEND-COND @ SIGNAL-COND TRUE
  ELSE
    DROP 0 FALSE
  THEN ;

\ Attempt to receive a value from a bounded channel without dequeueing any
\ values, returning FALSE if the bounded channel is empty.
: TRY-PEEK-BCHAN ( chan -- x found )
  DUP BCHAN-QUEUE-COUNT @ 0<> IF
    BCHAN-QUEUE @ @ TRUE
  ELSE
    DROP 0 FALSE
  THEN ;

\ Get the number of values queued in a bounded channel.
: COUNT-BCHAN ( chan -- u ) BCHAN-QUEUE-COUNT @ ;

\ Get whether a bounded channel is empty.
: EMPTY-BCHAN? ( chan -- empty ) BCHAN-QUEUE-COUNT @ 0 = ;

\ Get whether a bounded channel is full.
: FULL-BCHAN? ( chan -- full )
  DUP BCHAN-QUEUE-COUNT @ SWAP BCHAN-QUEUE-SIZE @ = ;

BASE ! SET-CURRENT SET-ORDER