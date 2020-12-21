# https://github.com/aymericdamien/TensorFlow-Examples/blob/master/tensorflow_v2/notebooks/3_NeuralNetworks/neural_network_raw.ipynb

from __future__ import absolute_import, division, print_function
import tensorflow as tf
import numpy as np
import json

def loadData():
    with open("D:/temp/recording/sino-ml.json") as file:
        training_sets = json.load(file)

    inputs = []
    outputs = []
    
    for elem in training_sets["dataset"]:
        inputs.append(elem["input"])
        outputs.append(elem["output"])

    return inputs, outputs

def createArrCode(tfVar, name):
    desc = name + " = new float[]{ ";
    desc += "%ff" % (tfVar[0])
    for idx in range(1, tfVar.shape[0]):
        desc += ", %ff" % (tfVar[idx])
    desc += " };"
    return desc

def writeCodeFile(lines):
    with open("D:/temp/recording/sino-ml-code.txt", "w") as file:
        for line in lines:
            file.write(line)
            file.write("\n")
        
# ----------------------
num_classes = 4
num_features = 16*16

# Training parameters.
learning_rate = 0.001
training_steps = 3000
batch_size = 256
display_step = 100

n_hidden_1 = 40 # 1st layer number of neurons.
#n_hidden_2 = 256 # 2nd layer number of neurons

# load data, x_train: normalized inputs, y_train: labels
x_train, y_train = loadData()
x_train = np.array(x_train, np.float32)
x_train = x_train.reshape([-1, num_features])

train_data = tf.data.Dataset.from_tensor_slices((x_train, y_train))
train_data = train_data.repeat().shuffle(5000).batch(batch_size).prefetch(1)

# Store layers weight & bias
random_normal = tf.initializers.RandomNormal()

weights = {
    'h1': tf.Variable(random_normal([num_features, n_hidden_1])),
    'out': tf.Variable(random_normal([n_hidden_1, num_classes]))
}
biases = {
    'b1': tf.Variable(tf.zeros([n_hidden_1])),
    'out': tf.Variable(tf.zeros([num_classes]))
}

# Create model.
def neural_net(x):
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
def cross_entropy(y_pred, y_true):
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
optimizer = tf.optimizers.SGD(learning_rate)

# Optimization process. 
def run_optimization(x, y):
    # Wrap computation inside a GradientTape for automatic differentiation.
    with tf.GradientTape() as g:
        pred = neural_net(x)
        loss = cross_entropy(pred, y)
        
    # Variables to update, i.e. trainable variables.
    trainable_variables = list(weights.values()) + list(biases.values())

    # Compute gradients.
    gradients = g.gradient(loss, trainable_variables)
    
    # Update W and b following gradients.
    optimizer.apply_gradients(zip(gradients, trainable_variables))

# Run training for the given number of steps.
for step, (batch_x, batch_y) in enumerate(train_data.take(training_steps), 1):
    # Run the optimization to update W and b values.
    run_optimization(batch_x, batch_y)
    
    if step % display_step == 0:
        pred = neural_net(batch_x)
        loss = cross_entropy(pred, batch_y)
        acc = accuracy(pred, batch_y)
        print("step: %i, loss: %f, accuracy: %f" % (step, loss, acc))

# Test model on validation set.
#pred = neural_net(x_test)
#print("Test Accuracy: %f" % accuracy(pred, y_test))

lin_weights_h1 = tf.reshape(weights['h1'], [-1])
lin_weights_out = tf.reshape(weights['out'], [-1])
lin_biases_h1 = tf.reshape(biases['b1'], [-1])
lin_biases_out = tf.reshape(biases['out'], [-1])

print(lin_weights_h1.shape)
print(lin_weights_out.shape)
print(lin_biases_h1.shape)
print(lin_biases_out.shape)

# Write data
outLines = []
outLines.append(createArrCode(lin_weights_h1, "WeightH1"))
outLines.append(createArrCode(lin_weights_out, "WeightOut"))
outLines.append(createArrCode(lin_biases_h1, "BiasH1"))
outLines.append(createArrCode(lin_biases_out, "BiasOut"))
writeCodeFile(outLines)
