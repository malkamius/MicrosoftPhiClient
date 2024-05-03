from flask import Flask, request, jsonify
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline
import threading

app = Flask(__name__)
lock = threading.Lock()

# Initialize the model globally
def initialize_model():
    global model, tokenizer, pipe
    torch.manual_seed(0)  # Use deterministic seed for reproducibility
    model = AutoModelForCausalLM.from_pretrained(
        "microsoft/Phi-3-mini-128k-instruct",
        device_map="cuda",
        torch_dtype="auto",
        trust_remote_code=True,
        attn_implementation="eager",
    )
    tokenizer = AutoTokenizer.from_pretrained("microsoft/Phi-3-mini-128k-instruct")
    pipe = pipeline("text-generation", model=model, tokenizer=tokenizer)  # Ensure device matches

initialize_model()

@app.route('/generate', methods=['POST'])
def generate():
    if not request.is_json:
        return "Request body must be JSON", 400

    data = request.get_json()
    try:
        with lock:
            messages = data['messages']
            lasttext = data['lasttext']
            prompt = pipe.tokenizer.apply_chat_template(
                messages,
                tokenize=False,
                add_generation_prompt=True
            ) + lasttext

            prompt_tokens = tokenizer.encode(prompt, return_tensors="pt")
            prompt_length = prompt_tokens.shape[1] - 1
            #print(f"Prompt length in tokens: {prompt_length}")
            outputs = pipe(prompt, max_new_tokens=5, return_tensors="pt")
            output_tokens = outputs[0]["generated_token_ids"]  # Assuming a single batch output
            #print(f"Generated tokens: {output_tokens}")

            # Exclude prompt tokens from the output
            old_tokens = output_tokens[:prompt_length]
            new_tokens = output_tokens[prompt_length:]
            #print(f"New tokens: {new_tokens}")
            
            total_text = tokenizer.decode(output_tokens, skip_special_tokens=True)
            
            print("All text:", total_text)
            skip_text = tokenizer.decode(old_tokens, skip_special_tokens=True);
            print("Skip:", skip_text)
            start_index = len(skip_text)
            generated_text = total_text[start_index:]
            print("Left over:", generated_text)

            if tokenizer.eos_token_id in new_tokens:
                print("End of text")
                return jsonify(generated_text + "--end of text")
            else:
                return jsonify(generated_text)
    except KeyError as e:
        print(f"Missing key in JSON data: {e}")
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        print(f"Error processing request: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=False, port=9090)