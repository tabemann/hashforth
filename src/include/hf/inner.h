/* Copyright (c) 2018-2019, Travis Bemann
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE. */

#ifndef HF_INNER_H
#define HF_INNER_H

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include "hf/common.h"

/* Declarations */

/* Initialize hashforth */
void hf_init(hf_global_t* global);

/* Set an interrupt */
void hf_set_int(hf_global_t* global, hf_cell_t interrupt, hf_cell_t required);

/* The inner interpreter */
void hf_inner(hf_global_t* global);

/* Handle interrupts and execute the inner loop */
void hf_inner_and_recover(hf_global_t* global);

/* Boot the Forth VM */
void hf_boot(hf_global_t* global);

/* Allocate more user space if needed */
void hf_new_user_space(hf_global_t* global);

/* Guarantee user space is available */
void hf_guarantee(hf_global_t* global, hf_cell_t size);

/* Set user space pointer */
void hf_set_user_space(hf_global_t* global, void* user_space_new);

/* Allocate data in user space */
void* hf_allocate(hf_global_t* global, hf_cell_t size);

/* Allocate a word */
hf_word_t* hf_new_word(hf_global_t* global, hf_full_token_t token);

/* Allocate a service */
hf_sys_t* hf_new_service(hf_global_t* global, hf_sys_index_t index);

/* Allocate a token */
hf_full_token_t hf_new_token(hf_global_t* global);

#endif /* HF_INNER_H */

