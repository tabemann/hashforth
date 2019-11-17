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
forth-wordlist task-wordlist 2 set-order
task-wordlist set-current

begin-structure bufbchan-size
  field: bufbchan-flags
  field: bufbchan-recv-cond
  field: bufbchan-send-cond
  field: bufbchan-queue
  field: bufbchan-queue-count
  field: bufbchan-queue-size
  field: bufbchan-entry-size
  field: bufbchan-enqueue-index
  field: bufbchan-dequeue-index
end-structure

\ Bounded channel is allocated flag
1 constant allocated-bufbchan

\ Print out the internal values of a bounded channel.
: bufbchan. ( chan -- )
  cr ." bufbchan-flags: " dup bufbchan-flags @ .
  cr ." bufbchan-recv-cond: " dup bufbchan-recv-cond @ .
  cr ." bufbchan-send-cond: " dup bufbchan-send-cond @ .
  cr ." bufbchan-queue: " dup bufbchan-queue @ .
  cr ." bufbchan-queue-count: " dup bufbchan-queue-count @ .
  cr ." bufbchan-queue-size: " dup bufbchan-queue-size @ .
  cr ." bufbchan-entry-size: " dup bufbchan-entry-size @ .
  cr ." bufbchan-enqueue-index: " dup bufbchan-enqueue-index @ .
  cr ." bufbchan-dequeue-index: " bufbchan-dequeue-index @ . cr ;

\ Allot a new bounded channel with the specified queue size, entry size, and
\ condition variable queue size.
: allot-bufbchan ( queue-size entry-size cond-size -- chan )
  here bufbchan-size allot
  3 pick over bufbchan-queue-size !
  0 over bufbchan-flags !
  0 over bufbchan-queue-count !
  0 over bufbchan-enqueue-index !
  0 over bufbchan-dequeue-index !
  here 4 roll 4 pick * allot over bufbchan-queue !
  over allot-cond over bufbchan-recv-cond !
  swap allot-cond over bufbchan-send-cond !
  tuck bufbchan-entry-size ! ;

\ Allocate a new bounded channel with the specified queue size, entry size, and
\ condition variable queue size.
: allocate-bufbchan ( queue-size entry-size cond-size -- chan )
  bufbchan-size allocate!
  3 pick over bufbchan-queue-size !
  allocated-bufbchan over bufbchan-flags !
  0 over bufbchan-queue-count !
  0 over bufbchan-enqueue-index !
  0 over bufbchan-dequeue-index !
  3 roll 3 pick * allocate! over bufbchan-queue !
  over allocate-cond over bufbchan-recv-cond !
  swap allocate-cond over bufbchan-send-cond !
  tuck bufbchan-entry-size ! ;

\ Bounded channel is not allocated exception
: x-bufbchan-not-allocated ( -- ) space ." bufbchan is not allocated" cr ;

\ Destroy a bounded channel
: destroy-bufbchan ( chan -- )
  dup bufbchan-flags @ allocated-bufbchan and averts x-bufbchan-not-allocated
  dup bufbchan-send-cond @ destroy-cond
  dup bufbchan-recv-cond @ destroy-cond
  dup bufbchan-queue @ free!
  free! ;

\ Internal - dequeue a packet from a bounded channel; note that this does not
\ have any safeties for preventing dequeueing a value from an already empty
\ bounded channel, nor does this wake up any tasks waiting to send on the
\ bounded channel.
: dequeue-bufbchan ( addr chan -- )
  dup bufbchan-queue @ over bufbchan-dequeue-index @
  2 pick bufbchan-entry-size @ * + rot 2 pick bufbchan-entry-size @ cmove
  dup bufbchan-queue-count @ 1- over bufbchan-queue-count !
  dup bufbchan-dequeue-index @ 1+ over bufbchan-queue-size @ mod
  swap bufbchan-dequeue-index ! ;

\ Internal - enqueue a packet onto a bounded channel; note that this does not
\ have any safeties for preventing enqueuing a value onto an already full
\ bounded channel, nor does this wake up any tasks waiting to receiving on the
\ bounded channel.
: enqueue-bufbchan ( addr chan -- )
  tuck bufbchan-queue @ 2 pick bufbchan-enqueue-index @
  3 pick bufbchan-entry-size @ * + 2 pick bufbchan-entry-size @ cmove
  dup bufbchan-queue-count @ 1+ over bufbchan-queue-count !
  dup bufbchan-enqueue-index @ 1+ over bufbchan-queue-size @ mod
  swap bufbchan-enqueue-index ! ;

\ Internal - peek a value from a bounded channel; note that this does not have
\ any safeties for preventing peeking a value from an empty bounded channel.
: do-peek-bufbchan ( addr chan -- )
  dup bufbchan-queue @ over bufbchan-dequeue-index @
  2 pick bufbchan-entry-size @ * + rot rot bufbchan-entry-size @ cmove ;

\ Send a packet on a bounded channel, waking up one task waiting to receive a
\ packet from the bounded channel, and waiting for a value to be read from the
\ bounded channel if it is already full.
: send-bufbchan ( addr chan -- )
  begin-atomic
  begin dup bufbchan-queue-count @ over bufbchan-queue-size @ >= while
    dup bufbchan-send-cond @ end-atomic wait-cond begin-atomic
  repeat
  tuck enqueue-bufbchan bufbchan-recv-cond @ end-atomic signal-cond ;

\ Attempt to send a packet on a bounded channel, waking up one task waiting to
\ receive a packet from the bounded channel, and returning FALSE if the bounded
\ channel is already full.
: try-send-bufbchan ( addr chan -- success )
  begin-atomic
  dup bufbchan-queue-count @ over bufbchan-queue-size @ < if
    tuck enqueue-bufbchan bufbchan-recv-cond @ end-atomic signal-cond true
  else
    end-atomic 2drop false
  then ;

\ Receive a packet from a bounded channel, waking up one task waiting to send
\ a packet on the bounded channel, and waiting for a task to send a packet on
\ the bounded channel if it is empty.
: recv-bufbchan ( addr chan -- )
  begin-atomic
  begin dup bufbchan-queue-count @ 0= while
    dup bufbchan-recv-cond @ end-atomic wait-cond begin-atomic
  repeat
  tuck dequeue-bufbchan bufbchan-send-cond @ end-atomic signal-cond ;

\ Receive a packet from a bounded channel without dequeueing any packets,
\ waiting for a task to send a packet on the bounded channel if it is empty.
: peek-bufbchan ( addr chan -- )
  begin-atomic
  begin dup bufbchan-queue-count @ 0= while
    dup bufbchan-recv-cond @ end-atomic wait-cond begin-atomic
  repeat
  do-peek-bufbchan end-atomic ;

\ Attempt to receive a packet from a bounded channel, waking up one task waiting
\ to send a packet on the bounded channel, and returning FALSE if the bounded
\ channel is empty.
: try-recv-bufbchan ( addr chan -- found )
  begin-atomic
  dup bufbchan-queue-count @ 0<> if
    tuck dequeue-bufbchan bufbchan-send-cond @ end-atomic signal-cond true
  else
    end-atomic 2drop false
  then ;

\ Attempt to receive a packet from a bounded channel without dequeueing any
\ packets, returning FALSE if the bounded channel is empty.
: try-peek-bufbchan ( addr chan -- found )
  begin-atomic
  dup bufbchan-queue-count @ 0<> if
    do-peek-bufbchan true
  else
    2drop false
  then
  end-atomic ;

\ Get the number of values queued in a bounded channel.
: count-bufbchan ( chan -- u ) bufbchan-queue-count @ ;

\ Get whether a bounded channel is empty.
: empty-bufbchan? ( chan -- empty ) bufbchan-queue-count @ 0 = ;

\ Get whether a bounded channel is full.
: full-bufbchan? ( chan -- full )
  begin-atomic
  dup bufbchan-queue-count @ swap bufbchan-queue-size @ =
  end-atomic ;

\ Get bounded channel entry size.
: get-bufbchan-entry-size ( chan -- u ) bufbchan-entry-size @ ;

base ! set-current set-order