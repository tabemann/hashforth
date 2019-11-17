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
forth-wordlist vector-wordlist lambda-wordlist task-wordlist 4 set-order
task-wordlist set-current

\ Channel structure
begin-structure bufchan-size
  \ Receive condition for a channel
  field: bufchan-recv-cond

  \ Channel vector
  field: bufchan-vector
end-structure

\ Allocate a new channel with the specified initial queue size, entry
\ size, and condition variable queue size.
: allocate-bufchan ( queue-size entry-size cond-size -- chan )
  rot rot bufchan-size allocate!
  rot rot allocate-vector swap tuck bufchan-vector !
  swap allocate-cond swap tuck bufchan-recv-cond ! ;

\ Destroy a channel
: destroy-bufchan ( chan -- )
  dup bufchan-recv-cond @ destroy-cond
  dup bufchan-vector @ destroy-vector
  free! ;

\ Channel internal exception
: x-bufchan-internal ( -- ) space ." bufchan intenral exception" cr ;

\ Send a packet on a channel, waking up one task waiting to receive a packet
\ from the channel.
: send-bufchan ( addr chan -- )
  begin-atomic
  tuck bufchan-vector @ push-end-vector averts x-bufchan-internal
  bufchan-recv-cond @ end-atomic signal-cond ;

\ Receive a packet from a channel, waiting for a task to send a packet on the
\ channel if it is empty.
: recv-bufchan ( addr chan -- )
  begin-atomic
  begin dup bufchan-vector @ count-vector 0 = while
    dup bufchan-recv-cond @ end-atomic wait-cond begin-atomic 
  repeat
  bufchan-vector @ pop-start-vector averts x-bufchan-internal end-atomic ;

\ Receive a packet from a channel without dequeueing any packets, waiting for a
\ task to send a packet on the channel if it is empty.
: peek-bufchan ( addr chan -- )
  begin-atomic
  begin dup bufchan-vector @ count-vector 0 = while
    dup bufchan-recv-cond @ end-atomic wait-cond begin-atomic
  repeat
  bufchan-vector @ peek-start-vector averts x-bufchan-internal end-atomic ;

\ Attempt to receive a packet from a channel, returning FALSE if the channel is
\ empty.
: try-recv-bufchan ( addr chan -- found )
  begin-atomic
  dup bufchan-vector @ count-vector @ 0 <> if
    bufchan-vector @ pop-start-vector averts x-bufchan-internal true
  else
    2drop false
  then
  end-atomic ;

\ Attempt to receive a packet from a channe without dequeueing any packets,
\ returning FALSE if the bounded channel is empty.
: try-peek-bufchan ( addr chan -- found )
  begin-atomic
  dup bufchan-vector @ count-vector @ 0 <> if
    bufchan-vector @ pop-start-vector averts x-bufchan-internal true
  else
    2drop false
  then
  end-atomic ;

\ Get the number of values queues in a channel.
: count-bufchan ( chan -- u )
  begin-atomic bufchan-vector @ count-vector end-atomic ;

\ Get whether a channel is empty.
: empty-bufchan? ( chan -- empty ) count-bufchan 0 = ;

\ Get channel entry size.
: get-bufchan-entry-size ( chan -- )
  begin-atomic bufchan-vector @ get-vector-entry-size end-atomic ;

base ! set-current set-order
