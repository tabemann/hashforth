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

begin-structure bchan-size
  field: bchan-flags
  field: bchan-recv-cond
  field: bchan-send-cond
  field: bchan-queue
  field: bchan-queue-count
  field: bchan-queue-size
  field: bchan-enqueue-index
  field: bchan-dequeue-index
end-structure

\ Bounded channel allocated flag
1 constant allocated-bchan

\ Print out the internal values of a bounded channel.
: bchan. ( chan -- )
  cr ." bchan-flags: " dup bchan-flags @ .
  cr ." bchan-recv-cond: " dup bchan-recv-cond @ .
  cr ." bchan-send-cond: " dup bchan-send-cond @ .
  cr ." bchan-queue: " dup bchan-queue @ .
  cr ." bchan-queue-count: " dup bchan-queue-count @ .
  cr ." bchan-queue-size: " dup bchan-queue-size @ .
  cr ." bchan-enqueue-index: " bchan-enqueue-index @ .
  cr ." bchan-dequeue-index: " bchan-dequeue-index @ . cr ;

\ Allot a new bounded channel with the specified queue size and condition
\ variable queue size.
: allot-bchan ( queue-size cond-size -- chan )
  swap
  here bchan-size allot
  2dup bchan-queue-size !
  0 over bchan-flags !
  0 over bchan-queue-count !
  0 over bchan-enqueue-index !
  0 over bchan-dequeue-index !
  here rot cells allot over bchan-queue !
  over allot-cond over bchan-recv-cond !
  swap allot-cond over bchan-send-cond ! ;

\ Allocate a new bounded channel with the specified queue size and condition
\ variable queue size.
: allocate-bchan ( queue-size cond-size -- chan )
  swap
  bchan-size allocate!
  2dup bchan-queue-size !
  allocated-bchan over bchan-flags !
  0 over bchan-queue-count !
  0 over bchan-enqueue-index !
  0 over bchan-dequeue-index !
  swap cells allocate! over bchan-queue !
  over allocate-cond over bchan-recv-cond !
  swap allocate-cond over bchan-send-cond ! ;

\ Bounded channel is not allocated exception
: x-bchan-not-allocated ( -- ) space ." bchan not allocated" cr ;

\ Destroy a bounded channel
: destroy-bchan ( chan -- )
  dup bchan-flags @ allocated-bchan and averts x-bchan-not-allocated
  dup bchan-send-cond @ destroy-cond
  dup bchan-recv-cond @ destroy-cond
  dup bchan-queue @ free!
  free! ;

\ Internal - dequeue a value from a bounded channel; note that this does not
\ have any safeties for preventing dequeueing a value from an already empty
\ bounded channel, nor does this wake up any tasks waiting to send on the
\ bounded channel.
: dequeue-bchan ( chan -- x )
  dup bchan-queue @ over bchan-dequeue-index @ cells + @
  over bchan-queue-count @ 1- 2 pick bchan-queue-count !
  over bchan-dequeue-index @ 1+ 2 pick bchan-queue-size @ mod
  rot bchan-dequeue-index ! ;

\ Internal - enqueue a value onto a bounded channel; note that this does not
\ have any safeties for preventing enqueuing a value onto an already full
\ bounded channel, nor does this wake up any tasks waiting to receiving on the
\ bounded channel.
: enqueue-bchan ( x chan -- )
  tuck bchan-queue @ 2 pick bchan-enqueue-index @ cells + !
  dup bchan-queue-count @ 1+ 2 pick bchan-queue-count !
  dup bchan-enqueue-index @ 1+ over bchan-queue-size @ mod
  swap bchan-enqueue-index ! ;

\ Internal - peek a value from a bounded channel; note that this does not have
\ any safeties for preventing peeking a value from an empty bounded channel.
: do-peek-bchan ( chan -- x )
  dup bchan-queue @ swap bchan-dequeue-index @ cells + @ ;

\ Send a value on a bounded channel, waking up one task waiting to receive a
\ value from the bounded channel, and waiting for a value to be read from the
\ bounded channel if it is already full.
: send-bchan ( x chan -- )
  begin-atomic
  begin dup bchan-queue-count @ over bchan-queue-size @ >= while
    dup bchan-send-cond @ end-atomic wait-cond begin-atomic
  repeat
  swap over enqueue-bchan bchan-recv-cond @ end-atomic signal-cond ;

\ Attempt to send a value on a bounded channel, waking up one task waiting to
\ receive a value from the bounded channel, and returning FALSE if the bounded
\ channel is already full.
: try-send-bchan ( x chan -- success )
  begin-atomic
  dup bchan-queue-count @ over bchan-queue-size @ < if
    tuck enqueue-bchan bchan-recv-cond @ end-atomic signal-cond true
  else
    end-atomic 2drop false
  then ;

\ Receive a value from a bounded channel, waking up one task waiting to send
\ a value on the bounded channel, and waiting for a task to send a value on
\ the bounded channel if it is empty.
: recv-bchan ( chan -- x )
  begin-atomic
  begin dup bchan-queue-count @ 0= while
    dup bchan-recv-cond @ end-atomic wait-cond begin-atomic
  repeat
  dup dequeue-bchan swap bchan-send-cond @ end-atomic signal-cond ;

\ Receive a value from a bounded channel without dequeueing any values, waiting
\ for a task to send a value on the bounded channel if it is empty.
: peek-bchan ( chan -- x )
  begin-atomic
  begin dup bchan-queue-count @ 0= while
    dup bchan-recv-cond @ end-atomic wait-cond begin-atomic
  repeat
  do-peek-bchan end-atomic ;

\ Attempt to receive a value from a bounded channel, waking up one task waiting
\ to send a value on the bounded channel, and returning FALSE if the bounded
\ channel is empty.
: try-recv-bchan ( chan -- x found )
  begin-atomic
  dup bchan-queue-count @ 0<> if
    dup dequeue-bchan swap bchan-send-cond @ end-atomic signal-cond true
  else
    end-atomic drop 0 false
  then ;

\ Attempt to receive a value from a bounded channel without dequeueing any
\ values, returning FALSE if the bounded channel is empty.
: try-peek-bchan ( chan -- x found )
  begin-atomic
  dup bchan-queue-count @ 0<> if
    do-peek-bchan true
  else
    drop 0 false
  then
  end-atomic ;

\ Get the number of values queued in a bounded channel.
: count-bchan ( chan -- u ) bchan-queue-count @ ;

\ Get whether a bounded channel is empty.
: empty-bchan? ( chan -- empty ) bchan-queue-count @ 0 = ;

\ Get whether a bounded channel is full.
: full-bchan? ( chan -- full )
  begin-atomic dup bchan-queue-count @ swap bchan-queue-size @ = end-atomic ;

base ! set-current set-order