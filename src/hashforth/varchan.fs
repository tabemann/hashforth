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
forth-wordlist lambda-wordlist task-wordlist 3 set-order
task-wordlist set-current

\ Variable-sized channel structure
begin-structure varchan-size
  \ Channel on which vaiable-sized channels are implemented
  field: varchan-bufchan

  \ Mutex to control access to sending on a variable-sized channel
  field: varchan-send-mutex

  \ Mutes to control access to reading on a variable-sized channel
  field: varchan-recv-mutex
end-structure

\ Variable-sized channel packet header structure
begin-structure varchan-header-size
  \ Variable-sized channel packet payload size
  field: varchan-remain-size
end-structure

\ Allocate a new variable-sized channel, with the specified queue size,
\ entry size, condition variable queue size, send mutex queue size, and
\ receive mutex queue size.
: allocate-varchan
  ( queue-size entry-size cond-size send-mutex-size recv-mutex-size -- chan )
  varchan-size allocate!
  tuck swap allocate-mutex swap varchan-recv-mutex !
  tuck swap allocate-mutex swap varchan-send-mutex !
  3 roll 3 roll varchan-header-size + 3 roll
  allocate-bufchan over varchan-bufchan ! ;

\ Destroy a variable-sized channel
: destroy-varchan ( chan -- )
  dup varchan-send-mutex @ destroy-mutex
  dup varchan-recv-mutex @ destroy-mutex
  dup varchan-bufchan @ destroy-bufchan
  free! ;

\ Get the maximum number of bytes to send per packet.
: get-varchan-max-data-size ( chan -- bytes )
  varchan-bufchan @ get-bufchan-entry-size varchan-header-size - ;

\ Advance HERE pointer
: advance-here ( chan -- )
  varchan-bufchan @ get-bufchan-entry-size allot ;

\ Retract HERE pointer
: retract-here ( chan -- )
  varchan-bufchan @ get-bufchan-entry-size negate allot ;

\ Get the size of the next series of packets, excluding headers and padding
: get-next-varchan-size ( chan -- bytes )
  here swap dup advance-here tuck varchan-bufchan @ peek-bufchan
  retract-here here varchan-remain-size @ ;

\ Get the number of bytes to send per packet.
: get-varchan-data-size ( bytes1 chan -- bytes2 )
  get-varchan-max-data-size swap min ;

\ Actually send a packet.
: (send-varchan) ( addr bytes chan -- )
  over 0 ?do
    over here varchan-remain-size !
    2dup get-varchan-data-size >r
    2 pick here varchan-header-size + r@ cmove
    rot r@ + rot r> - rot
    here over varchan-bufchan @ 2 pick advance-here send-bufchan
    dup retract-here
  dup get-varchan-max-data-size +loop
  drop 2drop ;

\ Send a series of packets.
: send-varchan ( addr bytes chan -- )
  dup varchan-send-mutex @ ['] (send-varchan) with-mutex ;

\ Actually receive a packet
: (recv-varchan) ( addr bytes1 chan -- bytes2 )
  0 >r begin
    over 0 > if
      here over varchan-bufchan @ 2 pick advance-here recv-bufchan
      dup retract-here
      here varchan-header-size + 3 pick
      here varchan-remain-size @ 3 pick get-varchan-data-size
      4 pick min >r r@ cmove rot r@ + rot r@ - rot r> r> + >r
      here varchan-remain-size @ over get-varchan-max-data-size <=
    else
      here over varchan-bufchan @ recv-bufchan
      here varchan-remain-size @ over get-varchan-max-data-size <=
    then
  until
  2drop drop r> ;

\ Receive a series of packets.
: recv-varchan ( addr bytes1 chan -- bytes2 )
  dup varchan-recv-mutex @ ['] (recv-varchan) with-mutex ;

\ Allocate space for and receive a series of packets.
: allocate-recv-varchan ( chan -- addr bytes )
  dup get-next-varchan-size dup >r allocate!
  dup r> 3 roll recv-varchan ;

base ! set-current set-order