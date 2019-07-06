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

\ The exiting actor implementation
: do-end-actor ( -- )
  8 0 ?do 1 0 sleep ." * " loop current-actor kill-actor ;

\ The actor implementation
: do-actor ( -- )
  begin
    recv-actor 2dup actor-check-end if
      current-actor (.) ." : " . drop free!
    else
      2drop free!
    then
  again ;

\ Spawn the exiting actor
default-actor-config ' do-end-actor spawn-actor constant end-actor

\ Spawn the actors
: spawn-actors ( -- )
  4 0 ?do
    default-actor-config ['] do-actor spawn-actor i cells actors + !
    i cells actors + @ end-actor subscribe-actor-end
  loop ;

spawn-actors

\ Start the actors
: start-actors ( -- )
  4 0 ?do i cells actors + @ start-actor loop
  end-actor start-actor ;
