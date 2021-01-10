# https://github.com/aymericdamien/TensorFlow-Examples/blob/master/tensorflow_v2/notebooks/3_NeuralNetworks/neural_network_raw.ipynb

from __future__ import absolute_import, division, print_function
import tensorflow as tf
import numpy as np
import json
import textwrap

# Create model.
def neural_net(x, weights, biases):
    # Hidden fully connected layer with 128 neurons.
    layer_1 = tf.add(tf.matmul(x, weights['h1']), biases['b1'])
    # Apply sigmoid to layer_1 output for non-linearity.
    layer_1 = tf.nn.sigmoid(layer_1)
    
    # Hidden fully connected layer with 256 neurons.
    #layer_2 = tf.add(tf.matmul(layer_1, weights['h2']), biases['b2'])
    # Apply sigmoid to layer_2 output for non-linearity.
    #layer_2 = tf.nn.sigmoid(layer_2)
    
    # Output fully connected layer with a neuron for each class.
    out_layer = tf.matmul(layer_1, weights['out']) + biases['out']
    # Apply softmax to normalize the logits to a probability distribution.
    return tf.nn.softmax(out_layer)

# Cross-Entropy loss function.
def cross_entropy(y_pred, y_true, num_classes):
    # Encode label to a one hot vector.
    y_true = tf.one_hot(y_true, depth=num_classes)
    # Clip prediction values to avoid log(0) error.
    y_pred = tf.clip_by_value(y_pred, 1e-9, 1.)
    # Compute cross-entropy.  
    return tf.reduce_mean(-tf.reduce_sum(y_true * tf.math.log(y_pred)))

# Accuracy metric.
def accuracy(y_pred, y_true):
    # Predicted class is the index of highest score in prediction vector (i.e. argmax).
    correct_prediction = tf.equal(tf.argmax(y_pred, 1), tf.cast(y_true, tf.int64))
    return tf.reduce_mean(tf.cast(correct_prediction, tf.float32), axis=-1)

# Stochastic gradient descent optimizer.
optimizer = tf.optimizers.SGD(learning_rate=0.001)

# Optimization process. 
def run_optimization(x, y, weights, biases, num_classes):
    # Wrap computation inside a GradientTape for automatic differentiation.
    with tf.GradientTape() as g:
        pred = neural_net(x, weights, biases)
        loss = cross_entropy(pred, y, num_classes)
        
    # Variables to update, i.e. trainable variables.
    trainable_variables = list(weights.values()) + list(biases.values())

    # Compute gradients.
    gradients = g.gradient(loss, trainable_variables)
    
    # Update W and b following gradients.
    optimizer.apply_gradients(zip(gradients, trainable_variables))

class NNTraining():
    def __init__(self, inputFile, outputFile):
        path = 'data/'
        self.inputFile = path + inputFile
        self.outputFile = path + outputFile
        pass
        
    def printToLines(self, prefix, values, suffix):
        longstr = prefix + ', '.join(('%ff' % v) for v in values) + suffix
        return textwrap.wrap(longstr, 250)

    def loadData(self):
        with open(self.inputFile) as file:
            training_sets = json.load(file)

        inputs = []
        outputs = []
        
        for elem in training_sets["dataset"]:
            inputs.append(elem["input"])
            outputs.append(elem["output"])

        return inputs, outputs
        

    def writeCodeFile(self, weights, biases):
        lin_weights_h1 = tf.reshape(weights['h1'], [-1])
        lin_weights_out = tf.reshape(weights['out'], [-1])
        lin_biases_h1 = tf.reshape(biases['b1'], [-1])
        lin_biases_out = tf.reshape(biases['out'], [-1])

        outLines = []
        outLines += self.printToLines('WeightH1 = new float[]{', lin_weights_h1, '};')
        outLines += self.printToLines('WeightOut = new float[]{', lin_weights_out, '};')
        outLines += self.printToLines('BiasH1 = new float[]{', lin_biases_h1, '};')
        outLines += self.printToLines('BiasOut = new float[]{', lin_biases_out, '};')
    
        with open(self.outputFile, "w") as file:
            for line in outLines:
                file.write(line)
                file.write("\n")


    def run(self, numFeatures, numClasses, numHidden1=40, numSteps=3000):
        # Training parameters.  
        training_steps = numSteps
        batch_size = 256
        display_step = 100
        
        n_hidden_1 = numHidden1 # 1st layer number of neurons.
        #n_hidden_2 = 256 # 2nd layer number of neurons

        # load data, x_train: normalized inputs, y_train: labels
        x_train, y_train = self.loadData()
        x_train = np.array(x_train, np.float32)
        x_train = x_train.reshape([-1, numFeatures])

        train_data = tf.data.Dataset.from_tensor_slices((x_train, y_train))
        train_data = train_data.repeat().shuffle(5000).batch(batch_size).prefetch(1)

        # Store layers weight & bias
        random_normal = tf.initializers.RandomNormal()

        weights = {
            'h1': tf.Variable(random_normal([numFeatures, n_hidden_1])),
            'out': tf.Variable(random_normal([n_hidden_1, numClasses]))
        }
        biases = {
            'b1': tf.Variable(tf.zeros([n_hidden_1])),
            'out': tf.Variable(tf.zeros([numClasses]))
        }

        # Run training for the given number of steps.
        for step, (batch_x, batch_y) in enumerate(train_data.take(training_steps), 1):
            # Run the optimization to update W and b values.
            run_optimization(batch_x, batch_y, weights, biases, numClasses)
            
            if step % display_step == 0:
                pred = neural_net(batch_x, weights, biases)
                loss = cross_entropy(pred, batch_y, numClasses)
                acc = accuracy(pred, batch_y)
                print("step: %i, loss: %f, accuracy: %f" % (step, loss, acc))

        self.writeCodeFile(weights, biases)
