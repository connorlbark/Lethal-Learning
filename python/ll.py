import tensorflow as tf
import tensorforce as tforce
import tensorforce.agents as agents
import keyboard
import numpy as np
import os
import socket

params = {
    "save_every": 100,
}

#[   0.            0.           -3.21633349    0.63          0.4998
#    1.            5.            0.            0.            0.
#    1.            0.            0.            0.            0.
#    0.            0.            0.            0.            0.
#    0.            0.            0.            0.            1.
#    0.            0.         -100.         -100.            0.
#    0.            0.            0.            0.            0.
#    0.            0.            0.            0.            0.
#    0.            0.            0.            0.            0.
#    0.            0.            0.            0.            1.
#    0.            0.         -100.         -100.        ]

class Agent:
    def __init__(self):
        self.action_map = {
            0: 'a',
            1: 's',
            2: 'd',
            3: 'w',
            4: 'space',
            5: 'c',
            6: 'c+a',
            7: 'c+s',
            8: 'c+d',
            9: 'c+w',
            10: 'x',
            11: 'z',
        }
        self.num_actions = len(self.action_map)
        self.last_action = [False] * self.num_actions
        self.num_state_items = 54
        self.agent = agents.Agent.create(
            agent='ppo',
            # Automatically configured network
            network='auto',
            # MDP structure
            states=dict(type='float', shape=(self.num_state_item, )),
            actions=dict(type='int', num_values=self.num_actions),
            # Optimization
            batch_size=10, update_frequency=2, learning_rate=1e-3, subsampling_fraction=0.2,
            optimization_steps=5,
            # Reward estimation
            likelihood_ratio_clipping=0.2, discount=0.99, estimate_terminal=False,
            # Critic
            critic_network='auto',
            critic_optimizer=dict(optimizer='adam', multi_step=10, learning_rate=1e-3),
            # Preprocessing
            preprocessing=None,
            # Exploration
            exploration=0.0, variable_noise=0.0,
            # Regularization
            l2_regularization=0.0, entropy_regularization=0.0,
            # TensorFlow etc
            name='agent', device=None, parallel_interactions=1, seed=None, execution=None, saver=None,
            summarizer=None, recorder=None
        )


    def update_state(self, state):
        action = self.agent.act(state)
        self.execute(action)

    def execute(self, action):
        for i in range(self.num_actions):
            if i == action:
                self.last_action[i] = self._send_keypress(self.last_action[i], True, self.action_map[i])
            else:
                self.last_action[i] = self._send_keypress(self.last_action[i], False, self.action_map[i])
    
    def _send_keypress(self, current_state, desired_state, key_signal):
        if current_state == desired_state:
            return current_state
        
        if current_state == False and desired_state == True:
            keyboard.press(key_signal)
            return desired_state
        
        if current_state == True and desired_state == False:
            keyboard.release(key_signal)
            return desired_state
    
    def start_new_episode(self):
        self.agent.reset()

    def save_to(self, directory):
        self.agent.save_model(directory=directory)
    
    def start_key_command(self):
        keyboard.press_and_release('enter')

def parse_line(items):
    out = np.empty((len(items),))
    for i, item in enumerate(items):
        out[i] = float(item)
    return out

def main():
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    addr = ('10.0.0.40', 3000)
    s.connect(addr)
    agent = Agent()

    ep_since_last_save = 0
    num_saved = len(os.listdir("models")) + 1
    while True:
        s.send(bytes('R', 'utf-8'))
        line = s.recv(512)
        data = line.split(b',')
        action = data[0].decode('utf-8')
        if action == "STATE":
            agent.update_state(parse_line(data[1:]))
        elif action == "END":
            ep_since_last_save += 1
            if ep_since_last_save >= params["save_every"]:
                os.mkdir("models/" + num_saved)
                agent.save_to("models/" + num_saved)
                ep_since_last_save = 0
                num_saved += 1
            agent.start_new_episode()
        elif action == "START":
            agent.start_key_command()


def debug_socket():
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    addr = ('10.0.0.40', 3000)
    s.connect(addr)
    while True:
        s.send(bytes('R', 'utf-8'))
        data = s.recv(512)
        spl = data.split(b',')
        print(spl[0].decode('utf-8') + ":")
        print(parse_line(spl[1:]))

if __name__ == "__main__":
    main()