from flask import Flask, request, jsonify
from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline
import threading
from config import DevelopmentConfig

app = Flask(__name__)
app.config.from_object(DevelopmentConfig)

lock = threading.Lock()
class TextGenerator:
    def __init__(self):
        self.initialize_model()

    def initialize_model(self):
        global lock
        with lock:
            self.model = AutoModelForCausalLM.from_pretrained(
                "leliuga/Phi-3-mini-128k-instruct-bnb-4bit",
                #"microsoft/Phi-3-mini-128k-instruct",
                device_map="cuda",
                torch_dtype="auto",
                trust_remote_code=True,
                attn_implementation="eager",
            )
            self.tokenizer = AutoTokenizer.from_pretrained("leliuga/Phi-3-mini-128k-instruct-bnb-4bit")
            self.pipe = pipeline("text-generation", model=self.model, tokenizer=self.tokenizer)

    def generate(self, messages, lasttext):
        global lock
        with lock:
            prompt = self.pipe.tokenizer.apply_chat_template(
                messages,
                tokenize=False,
                add_generation_prompt=True
            ) + lasttext
            prompt_tokens = self.tokenizer.encode(prompt, return_tensors="pt")
            prompt_length = prompt_tokens.shape[1] - 1
            outputs = self.pipe(prompt, max_new_tokens=50, return_tensors="pt")
            output_tokens = outputs[0]["generated_token_ids"]
            old_tokens = output_tokens[:prompt_length]
            #new_tokens = output_tokens[prompt_length:]
            total_text = self.tokenizer.decode(output_tokens, skip_special_tokens=True)
            skip_text = self.tokenizer.decode(old_tokens, skip_special_tokens=True);
            start_index = len(skip_text)
            generated_text = total_text[start_index:]
            return generated_text

text_generator = TextGenerator()

@app.route('/generate', methods=['POST'])
def generate_text():
    if not request.is_json:
        return jsonify({"error": "Request body must be JSON"}), 400

    data = request.get_json()
    try:
        messages = data['messages']
        lasttext = data['lasttext']
        generated_text = text_generator.generate(messages, lasttext)
        return jsonify(generated_text)
    except KeyError as e:
        print({"error": f"Missing key in JSON data: {e}"})
        return jsonify({"error": f"Missing key in JSON data: {e}"}), 400
    except Exception as e:
        print({"error": str(e)})
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=app.config['DEBUG'], port=app.config['PORT'])