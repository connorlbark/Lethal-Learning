import tensorflow as tf
import tensorforce as tforce
import tensorforce.agents as agents
import keyboard
import numpy as np
import os
import time
import socket
import datetime

params = {
    "save_every": 5,
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
            0: 'left',
            1: 'right',
            2: 'down',
            3: 'up',
            4: 'space',
            5: 'c',
            6: 'c+left',
            7: 'c+down',
            8: 'c+up',
            9: 'c+right',
            10: 'x',
            11: 'z',
            12: '',
        }
        self.num_actions = len(self.action_map)
        self.last_action = [False] * self.num_actions
        self.num_state_items = 54
        self.agent = agents.Agent.create(
            agent='ppo',
            # Automatically configured network
            network=dict(type='auto', depth=5, size=128),
            # MDP structure
            states=dict(type='float', shape=(54, )),
            actions=dict(type='int', num_values=13),
            max_episode_timesteps=60000, #memory=500000, 
            # Optimization
            batch_size=5, learning_rate=1e-3, subsampling_fraction=0.33,
            optimization_steps=10,
            # Reward estimation
            likelihood_ratio_clipping=0.2, discount=0.99, estimate_terminal=False,
            # Critic
            critic_network=dict(type='auto', depth=5, size=128),
            critic_optimizer=dict(optimizer='adam', multi_step=10, learning_rate=1e-3),
            # Preprocessing
            preprocessing=None,
            # Exploration
            exploration=0.0, variable_noise=0.05,
            # Regularization
            l2_regularization=0.2, entropy_regularization=0.0,
            # TensorFlow etc
            name='agent', device=None, parallel_interactions=1, seed=None, execution=None, saver=None,
            summarizer=None, recorder=None
        )
        """
        self.agent = agents.Agent.create(
            agent='dueling_dqn',
            states=dict(type='float', shape=(self.num_state_items, )),
            actions=dict(type='int', num_values=self.num_actions),
            max_episode_timesteps=18000,
            network=dict(type='auto', depth=5, size=128, internal_rnn=64),
            memory=20000,
        )"""


    def update_state(self, state):
        action = self.agent.act(state)
        self.execute(action)

        print(' '.join(map(str, zip(self.action_map.values(), self.last_action))))

    def execute(self, action):
        for i in range(self.num_actions):
            if i == action:
                self.last_action[i] = self._send_keypress(self.last_action[i], True, self.action_map[i])
            else:
                self.last_action[i] = self._send_keypress(self.last_action[i], False, self.action_map[i])
    
    def _send_keypress(self, current_state, desired_state, key_signal):
        if key_signal == '':
            return desired_state

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
        self.agent.save(directory=directory)
    
    def start_key_command(self):
        keyboard.press('enter')
        time.sleep(.1)
        keyboard.release('enter')
    
    def give_reward(self, reward, terminal):
        self.agent.observe(reward=reward, terminal=terminal)
    
    def stop_actions(self):
        for i in range(self.num_actions):
            self.last_action[i] = self._send_keypress(self.last_action[i], False, self.action_map[i])
        print("last:",self.last_action)

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
    first_state = True
    model_dir = os.path.join("models", datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
    os.mkdir(model_dir)
    num_saved = 0
    running = True
    print("Ready to run.")
    i = 0
    since_saved = 0
    while True:
        s.send(bytes('R', 'utf-8'))
        line = s.recv(1024)
        data = line.split(b',')
        action = data[0].decode('utf-8')

        if action == "START":
            print("START")
            time.sleep(5.0)
            agent.start_key_command()
            running = True
            continue

        reward = 0.0
        terminal = False
        if action == "STATE":
            reward = 0.0
        elif action == "WON":
            print("WON")
            #reward = 100.0
            terminal = True
        elif action == "LOST":
            print("LOST") 
            #reward = -100.0
            terminal = True
        elif action == "KILL":
            print("KILL")
            reward = 20.0
        elif action == "DEATH":
            print("DEATH")
            reward = -20.0
        if running:
            i += 1
            agent.update_state(parse_line(data[1:]))
            if terminal == True:
                print("Episode runs:", i)
                print("Stopping...")
                agent.stop_actions()
                print("Post-match training...")
            agent.give_reward(reward, terminal)
            if terminal == True:
                print("Saving...")
                if since_saved > params["save_every"]:
                    os.mkdir(os.path.join(model_dir, str(num_saved)))
                    agent.save_to(os.path.join(model_dir, str(num_saved)))
                    num_saved += 1
                running = False
                print("Resetting agent...")
                agent.start_new_episode()
                i = 0
            




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