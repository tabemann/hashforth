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

require src/hashforth/actor.fs

forth-wordlist task-wordlist 2 set-order

\ The maximum number of actors
4 constant max-actor-count

\ The actor array
create actors max-actor-count cells allot

\ The maximum number of cycles per actor
4 constant max-actor-cycles

\ Implement an actor
: actor create , does>
  dup @ 1 + max-actor-count umod
  max-actor-cycles 0 ?do
    recv-actor drop dup @ . free!
    1 0 sleep
    dup here ! here 1 cells allot 1 cells 2 pick cells actors + @ send-actor
    -1 cells allot
  loop
  drop ;

\ Instantiate a number of implementations of the actor
0 actor actor-0-word
1 actor actor-1-word
2 actor actor-2-word
3 actor actor-3-word

\ The exit listener actor implementation
: handler-actor-word ( -- )
  begin
    recv-actor 2dup actor-check-end if
      ." *" . drop free!
    else
      2drop free!
    then
  again ;

\ Create the actors
default-actor-config ' handler-actor-word spawn-actor constant handler-actor
default-actor-config ' actor-0-word spawn-actor 0 cells actors + !
default-actor-config ' actor-1-word spawn-actor 1 cells actors + !
default-actor-config ' actor-2-word spawn-actor 2 cells actors + !
default-actor-config ' actor-3-word spawn-actor 3 cells actors + !

\ Subscribe to actor termination
handler-actor 0 cells actors + @ subscribe-actor-end
handler-actor 1 cells actors + @ subscribe-actor-end
handler-actor 2 cells actors + @ subscribe-actor-end
handler-actor 3 cells actors + @ subscribe-actor-end

\ Start the actors
handler-actor start-actor
0 cells actors + @ start-actor
1 cells actors + @ start-actor
2 cells actors + @ start-actor
3 cells actors + @ start-actor

\ Send the initial message
: send-initial ( -- )
  0 here ! here 1 cells allot 1 cells 0 cells actors + @ send-actor
  -1 cells allot ;

send-initial
